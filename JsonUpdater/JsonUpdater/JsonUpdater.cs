using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Net.Http;

namespace JsonUpdater
{
    public class JsonUpdater
    {
        public class JsonGameInfo
        {
            public string product_id { get; set; }
            public string product_url { get; set; }
            public string title { get; set; }
            public string circle { get; set; }
            public string release_date { get; set; }
            public string category { get; set; }
            public List<string> genre { get; set; }
            public string main_image_url { get; set; }
        }

        public class UpdateFromJson : GenericPlugin
        {
            private static readonly ILogger logger = LogManager.GetLogger();
            // 一意識別用GUID（必要なら自分で変更OK）
            public override Guid Id { get; } = Guid.Parse("b07a97c3-bf3b-4ac9-97d0-f232bc18b999");
            public UpdateFromJson(IPlayniteAPI api) : base(api)
            {
            }
            public override IEnumerable<TopPanelItem> GetTopPanelItems()
            {
                var menu = new TopPanelItem
                {
                    Title = "JSONメタデータ更新",
                    Icon = null,
                    Activated = () => UpdateGames()
                };
                return new[] { menu };
            }

            private void UpdateGames()
            {
                var games = PlayniteApi.Database.Games.ToList();
                // tempフォルダ取得(ログ用)
                string tempPath = Path.GetTempPath();
                string logfile = Path.Combine(tempPath, "JsonUpdater.log");
                File.AppendAllText(logfile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 更新開始 {Environment.NewLine}");
                // ゲームごとに処理
                foreach (var game in games)
                {
                    // 既に設定している場合はスキップ
                    if (!string.IsNullOrEmpty(game.Description) && game.Description.Contains("[製品ID:"))
                    {
                        File.AppendAllText(logfile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {game.Name}は既に設定されているためスキップ {Environment.NewLine}");
                        continue;
                    }
                    File.AppendAllText(logfile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {game.Name}の読込開始 {Environment.NewLine}");
                    // インストールディレクトリを取得
                    var gameDir = game.InstallDirectory;
                    if (string.IsNullOrEmpty(gameDir)) continue;
                    // jsonファイルを探す
                    var jsonFile = FindJsonFile(gameDir);
                    if (string.IsNullOrEmpty(jsonFile)) continue;
                    var jsonText = File.ReadAllText(jsonFile);
                    JsonGameInfo info = null;
                    try
                    {
                        info = JsonConvert.DeserializeObject<JsonGameInfo>(jsonText);
                    }
                    catch (Exception)
                    {
                        File.AppendAllText(logfile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] jsonファイルが正しくないためスキップ {Environment.NewLine}");
                        continue;
                    }
                    if (info is null) continue;
                    File.AppendAllText(logfile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] jsonファイル取込完了 {Environment.NewLine}");
                    // メタデータを更新
                    // タイトル
                    if (!string.IsNullOrEmpty(info.title))
                    {
                        game.Name = info.title;
                    }
                    // サークル
                    if (!string.IsNullOrEmpty(info.circle))
                    {
                        var company = PlayniteApi.Database.Companies.FirstOrDefault(c => c.Name == info.circle);
                        if (company is null)
                        {
                            company = new Company(info.circle);
                            PlayniteApi.Database.Companies.Add(company);
                        }
                        if (game.DeveloperIds is null)
                        {
                            game.DeveloperIds = new List<Guid>();
                        }
                        game.DeveloperIds.Add(company.Id);
                    }
                    // パブリッシャー（サークルと同じ扱い）
                    if (!string.IsNullOrEmpty(info.circle))
                    {
                        var company = PlayniteApi.Database.Companies.FirstOrDefault(c => c.Name == info.circle);
                        if (company is null)
                        {
                            company = new Company(info.circle);
                            PlayniteApi.Database.Companies.Add(company);
                        }
                        if (game.PublisherIds is null)
                        {
                            game.PublisherIds = new List<Guid>();
                        }
                        game.PublisherIds.Add(company.Id);
                    }
                    // リリース日
                    if (DateTime.TryParse(info.release_date, out DateTime releaseDate))
                    {
                        game.ReleaseDate = new ReleaseDate(releaseDate);
                    }
                    // カテゴリ(ジャンル)
                    if (!string.IsNullOrEmpty(info.category))
                    {
                        var category = PlayniteApi.Database.Categories.FirstOrDefault(c => c.Name == info.category);
                        if (category is null)
                        {
                            category = new Category(info.category);
                            PlayniteApi.Database.Categories.Add(category);
                        }
                        if (game.CategoryIds is null)
                        {
                            game.CategoryIds = new List<Guid>();
                        }
                        game.CategoryIds.Add(category.Id);
                    }
                    // タグ
                    if (info.genre != null)
                    {
                        foreach (var g in info.genre)
                        {
                            var tag = PlayniteApi.Database.Tags.FirstOrDefault(t => t.Name == g);
                            if (tag is null)
                            {
                                tag = new Tag(g);
                                PlayniteApi.Database.Tags.Add(tag);
                            }
                            if (game.TagIds is null)
                            {
                                game.TagIds = new List<Guid>();
                            }
                            game.TagIds.Add(tag.Id);
                        }
                    }
                    // 画像
                    if (!string.IsNullOrEmpty(info.main_image_url) && info.main_image_url != "画像不明")
                    {
                        var imageUrl = DownloadImageToFile(info.main_image_url, gameDir);
                        string localImagePath = imageUrl;
                        game.BackgroundImage = localImagePath;
                    }
                    // 説明
                    game.Description = $"[製品ID: {info.product_id}]\n[製品URL: {info.product_url}]";
                    // データベースを更新
                    PlayniteApi.Database.Games.Update(game);
                    File.AppendAllText(logfile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {game.Name}の更新完了 {Environment.NewLine}");
                }
                PlayniteApi.Dialogs.ShowMessage("更新完了", "Playnite JSON 更新");
            }
            /// <summary>
            /// jsonファイルをディレクトリから再帰的に探す
            /// </summary>
            /// <param name="directory"></param>
            /// <returns></returns>
            public string FindJsonFile(string directory)
            {
                var currentDir = new DirectoryInfo(directory);
                while (currentDir != null)
                {
                    try
                    {
                        var jsonFile = Directory.GetFiles(currentDir.FullName, "folder_metadata.json").FirstOrDefault();
                        if (jsonFile != null)
                        {
                            return jsonFile;
                        }
                        currentDir = currentDir.Parent;
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
                return null;
            }

            /// <summary>
            /// 画像をURLからダウンロードしてローカルに保存する
            /// </summary>
            /// <param name="url"></param>
            /// <param name="saveDir"></param>
            /// <returns></returns>
            public string DownloadImageToFile(string url, string saveDir)
            {
                var fileName = Path.Combine(saveDir, "img_main.jpg");

                using (var client = new HttpClient())
                {
                    var data = client.GetByteArrayAsync(url).Result;
                    File.WriteAllBytes(fileName, data);
                }
                return fileName; // 保存したローカルパスを返す
            }
        }
    }
}
