# The trailer's own texts in the 34 languages of the showcase video (the rest come from i18n_a/b/c.py).
# {pm} = the game's own name for its Peaceful Mode in that language, {kind} = Kind Mita's official name (lang/glossary.json).
#   unofficial   the badge on the title card, the corner mark and the title: "Unofficial fan mod"
#   notofficial  the line under the title, and the description's first line: not AIHASTO's official mode
#   gift, music, battery, story   captions of what the showcase video didn't have yet
#   tr_title     (optional) the YouTube title; without it: "MiSide {pm} — {unofficial} | {subtitle}"
TR = {}

TR["ko"] = dict(
    unofficial="비공식 팬 모드",
    notofficial="AIHASTO의 공식 '{pm}'가 아닌, 팬이 만든 비공식 모드입니다",
    gift="선물하기 — 미타마다 좋아하는 선물이 다릅니다",
    music="특별한 시간 — 같이 음악 듣기",
    battery="노트북 배터리가 떨어지면 걱정해 줍니다",
    story="{kind}는 본편을 어디까지 했는지 기억합니다",
    tr_title="MiSide에 {pm}가 생겼습니다 (비공식 팬 모드) — 공포 없이 미타와 보내는 하루")
TR["en"] = dict(
    unofficial="Unofficial fan mod",
    notofficial="Not AIHASTO's official '{pm}': an unofficial mod made by a fan",
    gift="Gifts — every Mita has a favourite",
    music="Special time — listening to music together",
    battery="She worries when your laptop battery runs low",
    story="{kind} remembers how far you got in the story",
    tr_title="MiSide Finally Has a {pm} (Unofficial Fan Mod) — A Day with Mita, No Horror")
TR["ja"] = dict(
    unofficial="非公式ファンMOD",
    notofficial="AIHASTO公式の「{pm}」ではなく、ファンが作った非公式MODです",
    gift="プレゼント — ミタごとに好きな贈り物が違います",
    music="特別な時間 — 一緒に音楽を聴く",
    battery="ノートPCのバッテリーが減ると心配してくれます",
    story="{kind}は本編をどこまで進めたか覚えています",
    tr_title="MiSideに{pm}が登場（非公式ファンMOD）— ホラーなしでミタと過ごす一日")
TR["zh"] = dict(
    unofficial="非官方粉丝模组",
    notofficial="这不是AIHASTO官方的“{pm}”，而是粉丝制作的非官方模组",
    gift="送礼物 — 每位米塔喜欢的礼物都不同",
    music="特别时光 — 一起听音乐",
    battery="笔记本电量不足时，她会为你担心",
    story="{kind}记得你主线玩到了哪里",
    tr_title="MiSide终于有{pm}了（非官方粉丝模组）— 没有恐怖，与米塔共度的一天")
TR["zht"] = dict(
    unofficial="非官方粉絲模組",
    notofficial="這不是AIHASTO官方的「{pm}」，而是粉絲製作的非官方模組",
    gift="送禮物 — 每位米塔喜歡的禮物都不同",
    music="特別時光 — 一起聽音樂",
    battery="筆電電量不足時，她會為你擔心",
    story="{kind}記得你主線玩到了哪裡")
TR["ru"] = dict(
    unofficial="Неофициальный фанатский мод",
    notofficial="Это не официальный «{pm}» от AIHASTO, а неофициальный мод, сделанный фанатом",
    gift="Подарки — у каждой Миты свой любимый",
    music="Особое время — слушать музыку вместе",
    battery="Она волнуется, когда у ноутбука садится батарея",
    story="{kind} помнит, как далеко вы прошли сюжет")
TR["ruold"] = dict(
    unofficial="Неофиціальная любительская передѣлка",
    notofficial="Это не офиціальный «{pm}» отъ AIHASTO, а неофиціальная передѣлка, сдѣланная любителемъ",
    gift="Подарки — у каждой Миты свой любимый",
    music="Особое время — слушать музыку вмѣстѣ",
    battery="Она волнуется, когда у ноутбука садится батарея",
    story="{kind} помнитъ, какъ далеко вы прошли сюжетъ")
TR["uk"] = dict(
    unofficial="Неофіційний фанатський мод",
    notofficial="Це не офіційний «{pm}» від AIHASTO, а неофіційний мод, зроблений фанатом",
    gift="Подарунки — у кожної Міти свій улюблений",
    music="Особливий час — слухати музику разом",
    battery="Вона хвилюється, коли в ноутбука сідає батарея",
    story="{kind} пам'ятає, як далеко ви пройшли сюжет")
TR["be"] = dict(
    unofficial="Неафіцыйны фанацкі мод",
    notofficial="Гэта не афіцыйны «{pm}» ад AIHASTO, а неафіцыйны мод, зроблены фанатам",
    gift="Падарункі — у кожнай Міты свой улюбёны",
    music="Асаблівы час — слухаць музыку разам",
    battery="Яна хвалюецца, калі ў ноўтбука садзіцца батарэя",
    story="{kind} памятае, як далёка вы прайшлі сюжэт")
TR["bg"] = dict(
    unofficial="Неофициален фен мод",
    notofficial="Това не е официалният „{pm}“ на AIHASTO, а неофициален мод, направен от фен",
    gift="Подаръци — всяка Мита си има любим",
    music="Специално време — слушане на музика заедно",
    battery="Тя се тревожи, когато батерията на лаптопа пада",
    story="{kind} помни докъде сте стигнали в историята")
TR["kk"] = dict(
    unofficial="Бейресми жанкүйерлер моды",
    notofficial="Бұл AIHASTO-ның ресми «{pm}» емес, жанкүйер жасаған бейресми мод",
    gift="Сыйлықтар — әр Митаның өз сүйіктісі бар",
    music="Ерекше уақыт — бірге музыка тыңдау",
    battery="Ноутбуктің батареясы таусылып бара жатса, ол уайымдайды",
    story="{kind} негізгі оқиғаны қай жерге дейін өткеніңізді есінде сақтайды")
TR["sr"] = dict(
    unofficial="Nezvanični fanovski mod",
    notofficial="Ovo nije zvanični „{pm}“ studija AIHASTO, već nezvanični mod koji je napravio fan",
    gift="Pokloni — svaka Mita ima omiljeni",
    music="Posebno vreme — zajedničko slušanje muzike",
    battery="Brine se kad baterija laptopa oslabi",
    story="{kind} pamti dokle ste stigli u priči")
TR["hr"] = dict(
    unofficial="Neslužbeni fanovski mod",
    notofficial="Ovo nije službeni „{pm}“ studija AIHASTO, nego neslužbeni mod koji je izradio fan",
    gift="Darovi — svaka Mita ima omiljeni",
    music="Posebno vrijeme — zajedničko slušanje glazbe",
    battery="Brine se kad baterija prijenosnika oslabi",
    story="{kind} pamti dokle ste stigli u priči")
TR["cs"] = dict(
    unofficial="Neoficiální fanouškovský mod",
    notofficial="Toto není oficiální „{pm}“ od AIHASTO, ale neoficiální mod vytvořený fanouškem",
    gift="Dárky — každá Mita má svůj oblíbený",
    music="Zvláštní chvíle — společný poslech hudby",
    battery="Dělá si starosti, když notebooku dochází baterie",
    story="{kind} si pamatuje, jak daleko jste se dostali v příběhu")
TR["sk"] = dict(
    unofficial="Neoficiálny fanúšikovský mod",
    notofficial="Toto nie je oficiálny „{pm}“ od AIHASTO, ale neoficiálny mod vytvorený fanúšikom",
    gift="Darčeky — každá Mita má svoj obľúbený",
    music="Výnimočné chvíle — spoločné počúvanie hudby",
    battery="Robí si starosti, keď sa notebooku míňa batéria",
    story="{kind} si pamätá, ako ďaleko ste sa dostali v príbehu")
TR["pl"] = dict(
    unofficial="Nieoficjalny mod fanowski",
    notofficial="To nie jest oficjalny „{pm}” od AIHASTO, lecz nieoficjalny mod stworzony przez fana",
    gift="Prezenty — każda Mita ma swój ulubiony",
    music="Wyjątkowe chwile — wspólne słuchanie muzyki",
    battery="Martwi się, gdy w laptopie kończy się bateria",
    story="{kind} pamięta, jak daleko zaszedłeś w fabule")
TR["hu"] = dict(
    unofficial="Nem hivatalos rajongói mod",
    notofficial="Ez nem az AIHASTO hivatalos „{pm}” módja, hanem egy rajongó által készített nem hivatalos mod",
    gift="Ajándékok — minden Mitának megvan a kedvence",
    music="Különleges idő — közös zenehallgatás",
    battery="Aggódik, ha merül a laptopod akkumulátora",
    story="{kind} emlékszik, meddig jutottál a történetben")
TR["ro"] = dict(
    unofficial="Mod neoficial făcut de fani",
    notofficial="Acesta nu este „{pm}” oficial de la AIHASTO, ci un mod neoficial făcut de un fan",
    gift="Cadouri — fiecare Mita are unul preferat",
    music="Timp special — ascultăm muzică împreună",
    battery="Își face griji când bateria laptopului e pe terminate",
    story="{kind} ține minte cât de departe ai ajuns în poveste")
TR["de"] = dict(
    unofficial="Inoffizielle Fan-Mod",
    notofficial="Nicht der offizielle „{pm}“ von AIHASTO, sondern eine inoffizielle Mod von einem Fan",
    gift="Geschenke — jede Mita hat ein Lieblingsgeschenk",
    music="Besondere Zeit — gemeinsam Musik hören",
    battery="Sie sorgt sich, wenn dein Laptop-Akku zur Neige geht",
    story="{kind} weiß noch, wie weit du in der Story gekommen bist")
TR["fr"] = dict(
    unofficial="Mod de fan non officiel",
    notofficial="Ce n'est pas le « {pm} » officiel d'AIHASTO, mais un mod non officiel créé par un fan",
    gift="Cadeaux — chaque Mita a son préféré",
    music="Moment spécial — écouter de la musique ensemble",
    battery="Elle s'inquiète quand la batterie de votre portable faiblit",
    story="{kind} se souvient jusqu'où vous êtes allé dans l'histoire")
TR["it"] = dict(
    unofficial="Mod non ufficiale creata dai fan",
    notofficial="Non è la «{pm}» ufficiale di AIHASTO, ma una mod non ufficiale creata da un fan",
    gift="Regali — ogni Mita ha il suo preferito",
    music="Momenti speciali — ascoltare musica insieme",
    battery="Si preoccupa quando la batteria del portatile si scarica",
    story="{kind} ricorda fin dove sei arrivato nella storia")
TR["es"] = dict(
    unofficial="Mod no oficial hecho por fans",
    notofficial="No es el «{pm}» oficial de AIHASTO, sino un mod no oficial creado por un fan",
    gift="Regalos — cada Mita tiene su favorito",
    music="Momento especial — escuchar música juntos",
    battery="Se preocupa cuando a tu portátil le queda poca batería",
    story="{kind} recuerda hasta dónde llegaste en la historia")
TR["esla"] = dict(
    unofficial="Mod no oficial hecho por fans",
    notofficial="No es el «{pm}» oficial de AIHASTO, sino un mod no oficial creado por un fan",
    gift="Regalos — cada Mita tiene su favorito",
    music="Momento especial — escuchar música juntos",
    battery="Se preocupa cuando a tu laptop le queda poca batería",
    story="{kind} recuerda hasta dónde llegaste en la historia")
TR["pt"] = dict(
    unofficial="Mod não oficial feito por fãs",
    notofficial="Não é o «{pm}» oficial da AIHASTO, mas sim um mod não oficial criado por um fã",
    gift="Presentes — cada Mita tem o seu favorito",
    music="Momento especial — ouvir música juntos",
    battery="Ela preocupa-se quando a bateria do portátil está a acabar",
    story="{kind} lembra-se de até onde chegaste na história")
TR["ptbr"] = dict(
    unofficial="Mod não oficial feito por fãs",
    notofficial="Não é o “{pm}” oficial da AIHASTO, e sim um mod não oficial criado por um fã",
    gift="Presentes — cada Mita tem o seu favorito",
    music="Momento especial — ouvir música juntos",
    battery="Ela se preocupa quando a bateria do notebook está acabando",
    story="{kind} lembra até onde você chegou na história")
TR["sv"] = dict(
    unofficial="Inofficiell fanmod",
    notofficial="Inte AIHASTO:s officiella ”{pm}”, utan en inofficiell mod gjord av ett fan",
    gift="Presenter — varje Mita har sin favorit",
    music="Speciell stund — lyssna på musik tillsammans",
    battery="Hon blir orolig när datorns batteri håller på att ta slut",
    story="{kind} minns hur långt du kom i berättelsen")
TR["tr"] = dict(
    unofficial="Resmî olmayan hayran yapımı mod",
    notofficial="Bu, AIHASTO'nun resmî “{pm}” modu değil; bir hayranın yaptığı resmî olmayan bir mod",
    gift="Hediyeler — her Mita'nın bir favorisi var",
    music="Özel zaman — birlikte müzik dinlemek",
    battery="Dizüstü bilgisayarının pili azalınca endişeleniyor",
    story="{kind}, hikâyede nereye kadar geldiğini hatırlıyor")
TR["id"] = dict(
    unofficial="Mod tidak resmi buatan penggemar",
    notofficial="Ini bukan “{pm}” resmi dari AIHASTO, melainkan mod tidak resmi buatan penggemar",
    gift="Hadiah — tiap Mita punya favoritnya sendiri",
    music="Waktu spesial — mendengarkan musik bersama",
    battery="Dia khawatir saat baterai laptopmu hampir habis",
    story="{kind} ingat sejauh mana kamu memainkan ceritanya")
TR["fil"] = dict(
    unofficial="Hindi opisyal na fan-made na mod",
    notofficial="Hindi ito ang opisyal na “{pm}” ng AIHASTO, kundi isang hindi opisyal na mod na gawa ng fan",
    gift="Mga regalo — may paborito ang bawat Mita",
    music="Espesyal na oras — sabay na pakikinig ng musika",
    battery="Nag-aalala siya kapag paubos na ang baterya ng laptop mo",
    story="Naaalala ni {kind} kung hanggang saan ka umabot sa kuwento")
TR["vi"] = dict(
    unofficial="Bản mod không chính thức của người hâm mộ",
    notofficial="Đây không phải “{pm}” chính thức của AIHASTO mà là bản mod không chính thức do người hâm mộ làm",
    gift="Tặng quà — mỗi Mita thích một món khác nhau",
    music="Khoảnh khắc đặc biệt — cùng nghe nhạc",
    battery="Cô ấy lo lắng khi laptop của bạn sắp hết pin",
    story="{kind} nhớ bạn đã chơi cốt truyện đến đâu")
TR["th"] = dict(
    unofficial="ม็อดไม่เป็นทางการโดยแฟน ๆ",
    notofficial="นี่ไม่ใช่ “{pm}” อย่างเป็นทางการของ AIHASTO แต่เป็นม็อดไม่เป็นทางการที่แฟนทำขึ้นเอง",
    gift="ให้ของขวัญ — มิตะแต่ละคนชอบของขวัญไม่เหมือนกัน",
    music="ช่วงเวลาพิเศษ — ฟังเพลงด้วยกัน",
    battery="เธอเป็นห่วงเมื่อแบตเตอรี่โน้ตบุ๊กของคุณใกล้หมด",
    story="{kind}จำได้ว่าคุณเล่นเนื้อเรื่องหลักไปถึงไหนแล้ว")
TR["ar"] = dict(
    unofficial="تعديل غير رسمي من صنع المعجبين",
    notofficial="هذا ليس «{pm}» الرسمي من AIHASTO، بل تعديل غير رسمي صنعه أحد المعجبين",
    gift="الهدايا — لكل ميتا هديتها المفضلة",
    music="وقت خاص — الاستماع إلى الموسيقى معًا",
    battery="تقلق عليك عندما تنخفض بطارية حاسوبك المحمول",
    story="{kind} تتذكر إلى أين وصلت في القصة")
TR["arz"] = dict(
    unofficial="مود مش رسمي من عمل المعجبين",
    notofficial="ده مش «{pm}» الرسمي بتاع AIHASTO، ده مود مش رسمي عمله واحد من المعجبين",
    gift="الهدايا — كل ميتا ليها هدية بتحبها",
    music="وقت خاص — نسمع مزيكا مع بعض",
    battery="بتقلق عليك لما بطارية اللابتوب تقرّب تخلص",
    story="{kind} فاكرة إنت وصلت لحد فين في القصة")
TR["fa"] = dict(
    unofficial="ماد غیررسمی ساخت طرفداران",
    notofficial="این «{pm}» رسمی AIHASTO نیست؛ یک ماد غیررسمی است که یک طرفدار ساخته",
    gift="هدیه‌ها — هر میتا هدیهٔ محبوب خودش را دارد",
    music="زمان ویژه — با هم موسیقی گوش دادن",
    battery="وقتی باتری لپ‌تاپتان کم می‌شود نگران می‌شود",
    story="{kind} یادش هست داستان را تا کجا پیش برده‌اید")

# the description's download line (the address follows it on the next line)
DOWNLOAD = {
    "ko": "⬇ 내려받기 · 설치 (무료, 자동 설치 프로그램 포함)",
    "en": "⬇ Download and install (free, with an installer)",
    "ja": "⬇ ダウンロードとインストール（無料・インストーラー付き）",
    "zh": "⬇ 下载与安装（免费，附安装程序）",
    "zht": "⬇ 下載與安裝（免費，附安裝程式）",
    "ru": "⬇ Скачать и установить (бесплатно, есть установщик)",
    "ruold": "⬇ Скачать и установить (безплатно, съ установщикомъ)",
    "uk": "⬇ Завантажити та встановити (безкоштовно, є інсталятор)",
    "be": "⬇ Спампаваць і ўсталяваць (бясплатна, ёсць усталёўшчык)",
    "bg": "⬇ Изтегляне и инсталиране (безплатно, с инсталатор)",
    "kk": "⬇ Жүктеп алу және орнату (тегін, орнатқышы бар)",
    "sr": "⬇ Preuzimanje i instalacija (besplatno, sa instalerom)",
    "hr": "⬇ Preuzimanje i instalacija (besplatno, s instalacijskim programom)",
    "cs": "⬇ Stažení a instalace (zdarma, s instalátorem)",
    "sk": "⬇ Stiahnutie a inštalácia (zadarmo, s inštalátorom)",
    "pl": "⬇ Pobierz i zainstaluj (za darmo, z instalatorem)",
    "hu": "⬇ Letöltés és telepítés (ingyenes, telepítővel)",
    "ro": "⬇ Descarcă și instalează (gratuit, cu program de instalare)",
    "de": "⬇ Herunterladen und installieren (kostenlos, mit Installationsprogramm)",
    "fr": "⬇ Télécharger et installer (gratuit, avec un installateur)",
    "it": "⬇ Scarica e installa (gratis, con programma di installazione)",
    "es": "⬇ Descargar e instalar (gratis, con instalador)",
    "esla": "⬇ Descargar e instalar (gratis, con instalador)",
    "pt": "⬇ Descarregar e instalar (grátis, com instalador)",
    "ptbr": "⬇ Baixar e instalar (grátis, com instalador)",
    "sv": "⬇ Ladda ner och installera (gratis, med installationsprogram)",
    "tr": "⬇ İndir ve kur (ücretsiz, kurulum programıyla)",
    "id": "⬇ Unduh dan pasang (gratis, dengan penginstal)",
    "fil": "⬇ I-download at i-install (libre, may kasamang installer)",
    "vi": "⬇ Tải về và cài đặt (miễn phí, có trình cài đặt)",
    "th": "⬇ ดาวน์โหลดและติดตั้ง (ฟรี มีตัวติดตั้งให้)",
    "ar": "⬇ التنزيل والتثبيت (مجانًا، مع برنامج تثبيت)",
    "arz": "⬇ التحميل والتثبيت (ببلاش، ومعاه برنامج تثبيت)",
    "fa": "⬇ دانلود و نصب (رایگان، همراه با نصب‌کننده)",
}
REPO_URL = "https://github.com/O3O-2022/MiSide-PeaceMode"
