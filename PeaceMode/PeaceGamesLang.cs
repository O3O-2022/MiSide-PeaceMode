using System.Linq;

namespace MiSidePeaceMode
{
    // The word games can't be translated (a word chain and puns live in their own language): each language has its
    // own. Korean ones are in PeaceGames; English is last letter to first letter, Japanese is shiritori (hiragana),
    // Chinese is 词语接龙.
    public partial class PeaceDirector
    {
        static readonly string[][] WordChainsEn =
        {
            new[] { "apple", "egg", "giraffe", "elephant", "tiger", "rabbit", "tomato", "orange", "eagle", "engine" },
            new[] { "cat", "tea", "apron", "nest", "taxi", "ice", "eraser", "rose", "envelope" },
            new[] { "moon", "night", "tree", "egg", "guitar", "rain", "net", "toast" },
            new[] { "star", "robot", "tulip", "pancake", "energy", "yogurt", "tent", "tuna" },
            new[] { "piano", "owl", "lemon", "noodle", "earth", "honey", "yarn" },
        };
        static readonly string[] WordDecoysEn = { "banana", "cookie", "sugar", "window", "pillow", "candle", "muffin", "donut", "kitten", "bubble", "paper", "cloud", "spoon", "button", "ribbon", "mitten", "pencil", "sock", "cake", "lamp" };

        static readonly string[][] WordChainsJa =
        {
            new[] { "りんご", "ごりら", "らくだ", "だるま", "まくら", "らっぱ", "ぱせり", "りす" },
            new[] { "さくら", "らっこ", "こあら", "らじお", "おにぎり", "りか", "かめ", "めがね", "ねこ" },
            new[] { "すいか", "かさ", "さかな", "なす", "すずめ", "めだか", "かき", "きつね", "ねずみ" },
            new[] { "たまご", "ごま", "まど", "どんぐり", "りす", "すいとう", "うさぎ", "ぎんこう", "うみ" },
        };
        static readonly string[] WordDecoysJa = { "おかし", "ほし", "つき", "はな", "ゆき", "くも", "やま", "もも", "ひこうき", "てがみ", "あめ", "くつ", "はし", "えほん", "ふね" };

        static readonly string[][] WordChainsZh =
        {
            new[] { "天空", "空气", "气球", "球场", "场地", "地图", "图书", "书包", "包子" },
            new[] { "月亮", "亮光", "光明", "明天", "天气", "气味", "味道", "道路", "路口" },
            new[] { "学生", "生日", "日出", "出门", "门口", "口红", "红茶", "茶杯" },
            new[] { "火车", "车站", "站台", "台灯", "灯笼" },
        };
        static readonly string[] WordDecoysZh = { "蛋糕", "雨伞", "星星", "饼干", "音乐", "蝴蝶", "钢琴", "兔子", "西瓜", "雪人", "彩虹", "枕头", "蜡烛", "熊猫", "袜子" };

        static readonly (string q, string a, string[] wrong)[] RiddlesEn =
        {
            ("What has keys but can't open locks?", "A piano", new[] { "A door", "A jail" }),
            ("What gets wetter the more it dries?", "A towel", new[] { "A sponge", "Rain" }),
            ("What has hands but can't clap?", "A clock", new[] { "A glove", "A robot" }),
            ("What has to be broken before you can use it?", "An egg", new[] { "A promise", "A window" }),
            ("What goes up but never comes down?", "Your age", new[] { "A balloon", "A kite" }),
            ("What has a neck but no head?", "A bottle", new[] { "A shirt", "A swan" }),
            ("What can you catch but not throw?", "A cold", new[] { "A ball", "A fish" }),
            ("What has one eye but can't see?", "A needle", new[] { "A pirate", "A cyclops" }),
            ("What is full of holes but still holds water?", "A sponge", new[] { "A net", "A bucket" }),
            ("What starts with T, ends with T, and has T in it?", "A teapot", new[] { "A tent", "A toast" }),
            ("What has legs but doesn't walk?", "A table", new[] { "A snake", "A bird" }),
            ("What runs but never walks?", "Water", new[] { "A cheetah", "A clock" }),
            ("What building has the most stories?", "A library", new[] { "A skyscraper", "A hotel" }),
            ("What do you call a fish without an eye?", "A fsh", new[] { "A blind fish", "A shark" }),
            ("What word is spelled wrong in every dictionary?", "Wrong", new[] { "Dictionary", "Spelling" }),
        };

        static readonly (string q, string a, string[] wrong)[] RiddlesJa =
        {
            ("パンはパンでも食べられないパンは？", "フライパン", new[] { "食パン", "メロンパン" }),
            ("逆立ちすると軽くなる動物は？", "イルカ", new[] { "カメ", "サル" }),
            ("使えば使うほど小さくなるものは？", "消しゴム", new[] { "ノート", "定規" }),
            ("イカが怒ると何になる？", "イカリ", new[] { "タコ", "イカスミ" }),
            ("お父さんが嫌いな果物は？", "パパイヤ", new[] { "バナナ", "メロン" }),
            ("いつも寝ている動物は？", "ネコ", new[] { "イヌ", "ウサギ" }),
            ("一年に一度しか言わない挨拶は？", "あけましておめでとう", new[] { "おはよう", "ありがとう" }),
            ("朝は4本足、昼は2本足、夜は3本足。これなーんだ？", "人間", new[] { "犬", "椅子" }),
            ("上から読んでも下から読んでも同じ野菜は？", "トマト", new[] { "キャベツ", "ナス" }),
            ("目がたくさんあるのに何も見えないものは？", "網", new[] { "カメラ", "眼鏡" }),
            ("自分のものなのに、他人の方がよく使うものは？", "名前", new[] { "財布", "電話" }),
            ("歯がたくさんあるのに噛めないものは？", "くし", new[] { "ワニ", "歯ブラシ" }),
        };

        static readonly (string q, string a, string[] wrong)[] RiddlesZh =
        {
            ("什么东西越洗越脏？", "水", new[] { "衣服", "碗" }),
            ("什么路最窄？", "冤家路窄", new[] { "小路", "山路" }),
            ("天气越热，什么东西爬得越高？", "温度计", new[] { "太阳", "蜗牛" }),
            ("什么书在书店里买不到？", "秘书", new[] { "漫画书", "字典" }),
            ("什么人一年只工作一天？", "圣诞老人", new[] { "老师", "厨师" }),
            ("什么东西有眼却看不见？", "针", new[] { "猫头鹰", "镜子" }),
            ("小明的妈妈有三个孩子，老大叫大毛，老二叫二毛，老三叫什么？", "小明", new[] { "三毛", "小毛" }),
            ("什么东西要打破了才能用？", "鸡蛋", new[] { "窗户", "杯子" }),
            ("什么车寸步难行？", "风车", new[] { "火车", "马车" }),
            ("什么东西越生气越大？", "脾气", new[] { "气球", "声音" }),
            ("什么布剪不断？", "瀑布", new[] { "棉布", "抹布" }),
            ("什么桥下面没有水？", "立交桥", new[] { "石桥", "木桥" }),
        };

        static string[][] Chains => Loc.Lang switch { "en" => WordChainsEn, "ja" => WordChainsJa, "zh" => WordChainsZh, _ => WordChains };
        static string[] Decoys => Loc.Lang switch { "en" => WordDecoysEn, "ja" => WordDecoysJa, "zh" => WordDecoysZh, _ => WordDecoys };
        static (string q, string a, string[] wrong)[] RiddleSet => Loc.Lang switch { "en" => RiddlesEn, "ja" => RiddlesJa, "zh" => RiddlesZh, _ => Riddles };

        // the letter / syllable a word ends and starts with (English: case doesn't matter)
        static string Last(string w) => Loc.Lang == "en" ? w.Substring(w.Length - 1).ToLowerInvariant() : w.Substring(w.Length - 1);
        static string First(string w) => Loc.Lang == "en" ? w.Substring(0, 1).ToLowerInvariant() : w.Substring(0, 1);
    }
}
