# Playnite_JsonUpdater
Playniteに登録するタグを自動化する

DLstieとかで買ったゲームをPlayniteで登録するとき、いちいちタイトル等をPlaynite上で修正するのが面倒なのでこれを作成した。  
exeの登録自体は、Playniteでやるが、タグ付けはこれで行う。
 
以下のようなjsonファイルをexeと同じフォルダないし、上位のフォルダに入れておく  
ファイル名は`folder_metadata.json`
```json
{
    "product_id": "VJ004510",
    "product_url": "https://www.dlsite.com/soft/work/=/product_id/VJ004510.html",
    "title": "英雄伝説 空の軌跡FC",
    "circle": "Falcom",
    "release_date": "2012-04-13",
    "category": "ロールプレイング",
    "genre": [
        "ストーリーRPG",
        "ファンタジー"
    ],
    "main_image_url": "https://img.dlsite.jp/modpub/images2/work/professional/VJ005000/VJ004510_img_main.jpg"
}
```

自分はDLsiteで購入したものは`folder_metadata.json`を作っているのでこのようにしているが別にGOGとかでも使えると思う。




| タグ | 説明 |
| ---- | ---- |
| product_id | 説明項目に記載(空白でも可) |
| product_url | 説明項目の記載(空白でも可) |
| title | ゲームタイトル |
| circle | サークル(開発会社)、パブリッシャー両方登録 |
| release_date | リリース日 |
| category | ゲームカテゴリー(ジャンル) |
| genre | タグ(複数登録可能) |
| main_image_url | 代表画像 |
