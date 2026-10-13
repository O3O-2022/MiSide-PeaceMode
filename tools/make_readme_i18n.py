"""The read-me in every language the game has: docs/i18n/README.<code>.md (one short page each: what the mod is, what
is in it, how to install it) and the language bar at the top of the main README.md (Korean, the full one with a GIF
per feature).
The pages reuse the video's translated texts (tools/video/i18n_*.py: the description's feature list, the "not
official" line, the download line); only the install steps below are written for them.
usage: python tools/make_readme_i18n.py"""
import os, re, sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(REPO, "tools", "video"))
import assemble as A                       # (the texts in 34 languages, the glossary)
from i18n_trailer import TR, DOWNLOAD, REPO_URL

REL = REPO_URL + "/releases/latest"
# the pages' order, their file codes and the languages' own names
ORDER = ["en", "ja", "zh", "zht", "ru", "ruold", "uk", "be", "bg", "kk", "sr", "hr", "cs", "sk", "pl", "hu", "ro", "de", "fr",
         "it", "es", "esla", "pt", "ptbr", "sv", "tr", "id", "fil", "vi", "th", "ar", "arz", "fa"]
CODE = {"zh": "zh-Hans", "zht": "zh-Hant", "ruold": "ru-old", "sr": "sr-Latn", "esla": "es-419", "ptbr": "pt-BR", "arz": "ar-EG"}
RTL = {"ar", "arz", "fa"}

# name, "Install", three steps, where the full read-me is. {rel} the releases page, {main} the main read-me, {pm} the
# mode's name in that language. The button names are the installer's own (it has ten languages; English elsewhere).
S = {
    "en": ("English", "Install",
           "Download **MiSidePeaceMode-Setup** from [Releases]({rel}) and run it.",
           "Press **Install**: it finds the game folder and adds BepInEx (the mod loader) if it is missing.",
           "Start the game with **Start the game** or the desktop shortcut, then pick **{pm}** in the main menu.",
           "Every feature, with a GIF each, is in the [main README]({main}) (Korean)."),
    "ja": ("日本語", "インストール",
           "[Releases]({rel}) から **MiSidePeaceMode-Setup** をダウンロードして実行します。",
           "**インストール** を押すと、ゲームフォルダーを見つけ、BepInEx（MODローダー）がなければ自動で入れます。",
           "**ゲームを起動** ボタンかデスクトップのショートカットでゲームを起動し、メインメニューで **{pm}** を選びます。",
           "各機能のGIF付き紹介は[メインのREADME]({main})（韓国語）にあります。"),
    "zh": ("简体中文", "安装",
           "从 [Releases]({rel}) 下载 **MiSidePeaceMode-Setup** 并运行。",
           "点击 **安装**：它会找到游戏文件夹，如果没有 BepInEx（模组加载器）会自动装好。",
           "用 **启动游戏** 按钮或桌面快捷方式启动游戏，然后在主菜单选择 **{pm}**。",
           "每个功能的 GIF 演示见[主 README]({main})（韩语）。"),
    "zht": ("繁體中文", "安裝",
            "從 [Releases]({rel}) 下載 **MiSidePeaceMode-Setup** 並執行。",
            "按下 **安裝**：它會找到遊戲資料夾，若沒有 BepInEx（模組載入器）會自動裝好。",
            "用 **啟動遊戲** 按鈕或桌面捷徑啟動遊戲，然後在主選單選擇 **{pm}**。",
            "每個功能的 GIF 示範請見[主 README]({main})（韓文）。"),
    "ru": ("Русский", "Установка",
           "Скачайте **MiSidePeaceMode-Setup** со страницы [Releases]({rel}) и запустите.",
           "Нажмите **Установить**: программа найдёт папку игры и добавит BepInEx (загрузчик модов), если его нет.",
           "Запустите игру кнопкой **Запустить игру** или ярлыком на рабочем столе и выберите **{pm}** в главном меню.",
           "Все возможности с GIF — в [основном README]({main}) (на корейском)."),
    "ruold": ("Русскій (дореформенный)", "Установка",
              "Скачайте **MiSidePeaceMode-Setup** со страницы [Releases]({rel}) и запустите.",
              "Нажмите **Установить**: программа найдетъ папку игры и добавитъ BepInEx (загрузчикъ модовъ), если его нѣтъ.",
              "Запустите игру кнопкой **Запустить игру** или ярлыкомъ на рабочемъ столѣ и выберите **{pm}** въ главномъ меню.",
              "Всѣ возможности съ GIF — въ [основномъ README]({main}) (на корейскомъ)."),
    "uk": ("Українська", "Встановлення",
           "Завантажте **MiSidePeaceMode-Setup** зі сторінки [Releases]({rel}) і запустіть.",
           "Натисніть **Install**: програма знайде теку гри й додасть BepInEx (завантажувач модів), якщо його немає.",
           "Запустіть гру кнопкою **Start the game** або ярликом на робочому столі та виберіть **{pm}** у головному меню.",
           "Усі можливості з GIF — в [основному README]({main}) (корейською)."),
    "be": ("Беларуская", "Усталяванне",
           "Спампуйце **MiSidePeaceMode-Setup** са старонкі [Releases]({rel}) і запусціце.",
           "Націсніце **Install**: праграма знойдзе папку гульні і дадасць BepInEx (загрузчык модаў), калі яго няма.",
           "Запусціце гульню кнопкай **Start the game** або ярлыком на працоўным стале і выберыце **{pm}** у галоўным меню.",
           "Усе магчымасці з GIF — у [асноўным README]({main}) (па-карэйску)."),
    "bg": ("Български", "Инсталиране",
           "Изтеглете **MiSidePeaceMode-Setup** от страницата [Releases]({rel}) и го стартирайте.",
           "Натиснете **Install**: програмата намира папката на играта и добавя BepInEx (зареждача на модове), ако липсва.",
           "Стартирайте играта с бутона **Start the game** или с прекия път на работния плот и изберете **{pm}** в главното меню.",
           "Всички функции с GIF са в [основния README]({main}) (на корейски)."),
    "kk": ("Қазақша", "Орнату",
           "[Releases]({rel}) бетінен **MiSidePeaceMode-Setup** файлын жүктеп алып, іске қосыңыз.",
           "**Install** түймесін басыңыз: бағдарлама ойын қалтасын табады және BepInEx (мод жүктегіш) жоқ болса, оны қосады.",
           "Ойынды **Start the game** түймесімен немесе жұмыс үстеліндегі таңбашамен іске қосып, басты мәзірден **{pm}** таңдаңыз.",
           "Барлық мүмкіндіктер GIF-пен [негізгі README]({main}) бетінде (корей тілінде)."),
    "sr": ("Srpski", "Instalacija",
           "Preuzmite **MiSidePeaceMode-Setup** sa stranice [Releases]({rel}) i pokrenite ga.",
           "Kliknite na **Install**: program pronalazi folder igre i dodaje BepInEx (učitavač modova) ako ga nema.",
           "Pokrenite igru dugmetom **Start the game** ili prečicom na radnoj površini i izaberite **{pm}** u glavnom meniju.",
           "Sve funkcije sa GIF-ovima su u [glavnom README]({main}) (na korejskom)."),
    "hr": ("Hrvatski", "Instalacija",
           "Preuzmite **MiSidePeaceMode-Setup** sa stranice [Releases]({rel}) i pokrenite ga.",
           "Kliknite **Install**: program pronalazi mapu igre i dodaje BepInEx (učitavač modova) ako ga nema.",
           "Pokrenite igru gumbom **Start the game** ili prečacem na radnoj površini te odaberite **{pm}** u glavnom izborniku.",
           "Sve značajke s GIF-ovima nalaze se u [glavnom README]({main}) (na korejskom)."),
    "cs": ("Čeština", "Instalace",
           "Stáhněte **MiSidePeaceMode-Setup** ze stránky [Releases]({rel}) a spusťte jej.",
           "Klikněte na **Install**: program najde složku hry a přidá BepInEx (zavaděč modů), pokud chybí.",
           "Spusťte hru tlačítkem **Start the game** nebo zástupcem na ploše a v hlavním menu zvolte **{pm}**.",
           "Všechny funkce s GIFy najdete v [hlavním README]({main}) (korejsky)."),
    "sk": ("Slovenčina", "Inštalácia",
           "Stiahnite **MiSidePeaceMode-Setup** zo stránky [Releases]({rel}) a spustite ho.",
           "Kliknite na **Install**: program nájde priečinok hry a pridá BepInEx (zavádzač modov), ak chýba.",
           "Spustite hru tlačidlom **Start the game** alebo odkazom na ploche a v hlavnom menu zvoľte **{pm}**.",
           "Všetky funkcie s GIFmi nájdete v [hlavnom README]({main}) (po kórejsky)."),
    "pl": ("Polski", "Instalacja",
           "Pobierz **MiSidePeaceMode-Setup** ze strony [Releases]({rel}) i uruchom.",
           "Kliknij **Install**: program znajdzie folder gry i doda BepInEx (program ładujący mody), jeśli go brakuje.",
           "Uruchom grę przyciskiem **Start the game** lub skrótem na pulpicie i wybierz **{pm}** w menu głównym.",
           "Wszystkie funkcje z GIF-ami są w [głównym README]({main}) (po koreańsku)."),
    "hu": ("Magyar", "Telepítés",
           "Töltsd le a **MiSidePeaceMode-Setup** fájlt a [Releases]({rel}) oldalról, és futtasd.",
           "Kattints az **Install** gombra: megkeresi a játék mappáját, és hozzáadja a BepInExet (a modbetöltőt), ha hiányzik.",
           "Indítsd el a játékot a **Start the game** gombbal vagy az asztali parancsikonnal, majd a főmenüben válaszd a **{pm}** lehetőséget.",
           "Minden funkció GIF-fel a [fő README-ben]({main}) található (koreaiul)."),
    "ro": ("Română", "Instalare",
           "Descarcă **MiSidePeaceMode-Setup** de pe pagina [Releases]({rel}) și rulează-l.",
           "Apasă **Install**: găsește folderul jocului și adaugă BepInEx (încărcătorul de moduri) dacă lipsește.",
           "Pornește jocul cu butonul **Start the game** sau cu scurtătura de pe desktop și alege **{pm}** în meniul principal.",
           "Toate funcțiile, cu GIF-uri, sunt în [README-ul principal]({main}) (în coreeană)."),
    "de": ("Deutsch", "Installation",
           "Lade **MiSidePeaceMode-Setup** von der Seite [Releases]({rel}) herunter und starte es.",
           "Klicke auf **Installieren**: Das Programm findet den Spielordner und ergänzt BepInEx (den Mod-Loader), falls es fehlt.",
           "Starte das Spiel über **Spiel starten** oder die Desktopverknüpfung und wähle im Hauptmenü **{pm}**.",
           "Alle Funktionen mit GIFs stehen im [Haupt-README]({main}) (Koreanisch)."),
    "fr": ("Français", "Installation",
           "Téléchargez **MiSidePeaceMode-Setup** depuis la page [Releases]({rel}) et lancez-le.",
           "Cliquez sur **Installer** : le programme trouve le dossier du jeu et ajoute BepInEx (le chargeur de mods) s'il manque.",
           "Lancez le jeu avec le bouton **Lancer le jeu** ou le raccourci du bureau, puis choisissez **{pm}** dans le menu principal.",
           "Toutes les fonctionnalités, avec des GIF, sont dans le [README principal]({main}) (en coréen)."),
    "it": ("Italiano", "Installazione",
           "Scarica **MiSidePeaceMode-Setup** dalla pagina [Releases]({rel}) ed eseguilo.",
           "Premi **Install**: trova la cartella del gioco e aggiunge BepInEx (il caricatore di mod) se manca.",
           "Avvia il gioco con il pulsante **Start the game** o con il collegamento sul desktop e scegli **{pm}** nel menu principale.",
           "Tutte le funzioni, con GIF, sono nel [README principale]({main}) (in coreano)."),
    "es": ("Español", "Instalación",
           "Descarga **MiSidePeaceMode-Setup** desde la página [Releases]({rel}) y ejecútalo.",
           "Pulsa **Instalar**: encuentra la carpeta del juego y añade BepInEx (el cargador de mods) si falta.",
           "Inicia el juego con el botón **Iniciar el juego** o con el acceso directo del escritorio y elige **{pm}** en el menú principal.",
           "Todas las funciones, con GIF, están en el [README principal]({main}) (en coreano)."),
    "esla": ("Español (Latinoamérica)", "Instalación",
             "Descarga **MiSidePeaceMode-Setup** desde la página [Releases]({rel}) y ejecútalo.",
             "Haz clic en **Instalar**: encuentra la carpeta del juego y agrega BepInEx (el cargador de mods) si falta.",
             "Inicia el juego con el botón **Iniciar el juego** o con el acceso directo del escritorio y elige **{pm}** en el menú principal.",
             "Todas las funciones, con GIF, están en el [README principal]({main}) (en coreano)."),
    "pt": ("Português (Portugal)", "Instalação",
           "Descarrega **MiSidePeaceMode-Setup** na página [Releases]({rel}) e executa-o.",
           "Clica em **Instalar**: encontra a pasta do jogo e adiciona o BepInEx (o carregador de mods) se faltar.",
           "Inicia o jogo com o botão **Iniciar o jogo** ou com o atalho do ambiente de trabalho e escolhe **{pm}** no menu principal.",
           "Todas as funcionalidades, com GIF, estão no [README principal]({main}) (em coreano)."),
    "ptbr": ("Português (Brasil)", "Instalação",
             "Baixe **MiSidePeaceMode-Setup** na página [Releases]({rel}) e execute.",
             "Clique em **Instalar**: ele encontra a pasta do jogo e adiciona o BepInEx (o carregador de mods) se estiver faltando.",
             "Inicie o jogo com o botão **Iniciar o jogo** ou com o atalho da área de trabalho e escolha **{pm}** no menu principal.",
             "Todos os recursos, com GIFs, estão no [README principal]({main}) (em coreano)."),
    "sv": ("Svenska", "Installation",
           "Ladda ner **MiSidePeaceMode-Setup** från sidan [Releases]({rel}) och kör den.",
           "Klicka på **Install**: programmet hittar spelmappen och lägger till BepInEx (modladdaren) om den saknas.",
           "Starta spelet med knappen **Start the game** eller genvägen på skrivbordet och välj **{pm}** i huvudmenyn.",
           "Alla funktioner med GIF:ar finns i [huvud-README]({main}) (på koreanska)."),
    "tr": ("Türkçe", "Kurulum",
           "[Releases]({rel}) sayfasından **MiSidePeaceMode-Setup** dosyasını indirip çalıştırın.",
           "**Install** düğmesine basın: program oyun klasörünü bulur ve eksikse BepInEx'i (mod yükleyici) ekler.",
           "Oyunu **Start the game** düğmesiyle veya masaüstü kısayoluyla başlatın ve ana menüden **{pm}** seçeneğini seçin.",
           "Tüm özellikler GIF'leriyle [ana README]({main}) sayfasında (Korece)."),
    "id": ("Bahasa Indonesia", "Pemasangan",
           "Unduh **MiSidePeaceMode-Setup** dari halaman [Releases]({rel}) lalu jalankan.",
           "Klik **Install**: program menemukan folder game dan menambahkan BepInEx (pemuat mod) jika belum ada.",
           "Mulai game dengan tombol **Start the game** atau pintasan desktop, lalu pilih **{pm}** di menu utama.",
           "Semua fitur beserta GIF ada di [README utama]({main}) (bahasa Korea)."),
    "fil": ("Filipino", "Pag-install",
            "I-download ang **MiSidePeaceMode-Setup** mula sa pahinang [Releases]({rel}) at patakbuhin ito.",
            "I-click ang **Install**: hahanapin nito ang folder ng laro at idadagdag ang BepInEx (ang mod loader) kung wala pa.",
            "Simulan ang laro gamit ang **Start the game** o ang shortcut sa desktop, at piliin ang **{pm}** sa main menu.",
            "Ang lahat ng feature na may GIF ay nasa [pangunahing README]({main}) (Korean)."),
    "vi": ("Tiếng Việt", "Cài đặt",
           "Tải **MiSidePeaceMode-Setup** từ trang [Releases]({rel}) rồi chạy.",
           "Bấm **Install**: chương trình tìm thư mục game và thêm BepInEx (trình nạp mod) nếu chưa có.",
           "Mở game bằng nút **Start the game** hoặc lối tắt trên màn hình, rồi chọn **{pm}** ở menu chính.",
           "Mọi tính năng kèm GIF có trong [README chính]({main}) (tiếng Hàn)."),
    "th": ("ไทย", "การติดตั้ง",
           "ดาวน์โหลด **MiSidePeaceMode-Setup** จากหน้า [Releases]({rel}) แล้วเปิดใช้งาน",
           "กด **Install** โปรแกรมจะหาโฟลเดอร์เกมและเพิ่ม BepInEx (ตัวโหลดม็อด) ให้ถ้ายังไม่มี",
           "เปิดเกมด้วยปุ่ม **Start the game** หรือทางลัดบนเดสก์ท็อป แล้วเลือก **{pm}** ในเมนูหลัก",
           "ฟีเจอร์ทั้งหมดพร้อม GIF อยู่ใน [README หลัก]({main}) (ภาษาเกาหลี)"),
    "ar": ("العربية", "التثبيت",
           "نزّل **MiSidePeaceMode-Setup** من صفحة [Releases]({rel}) ثم شغّله.",
           "اضغط **Install**: يعثر البرنامج على مجلد اللعبة ويضيف BepInEx (محمّل المودات) إن لم يكن موجودًا.",
           "شغّل اللعبة بزر **Start the game** أو باختصار سطح المكتب، ثم اختر **{pm}** من القائمة الرئيسية.",
           "كل الميزات مع صور GIF في [ملف README الرئيسي]({main}) (بالكورية)."),
    "arz": ("العربية (مصر)", "التثبيت",
            "نزّل **MiSidePeaceMode-Setup** من صفحة [Releases]({rel}) وشغّله.",
            "دوس على **Install**: البرنامج هيلاقي فولدر اللعبة ويضيف BepInEx (محمّل المودات) لو مش موجود.",
            "شغّل اللعبة بزرار **Start the game** أو بالاختصار اللي على سطح المكتب، واختار **{pm}** من القايمة الرئيسية.",
            "كل المميزات بصور GIF في [ملف README الرئيسي]({main}) (بالكوري)."),
    "fa": ("فارسی", "نصب",
           "فایل **MiSidePeaceMode-Setup** را از صفحهٔ [Releases]({rel}) دانلود و اجرا کنید.",
           "روی **Install** بزنید: برنامه پوشهٔ بازی را پیدا می‌کند و اگر BepInEx (بارگذار ماد) نباشد آن را اضافه می‌کند.",
           "بازی را با دکمهٔ **Start the game** یا میان‌بر دسکتاپ اجرا کنید و در منوی اصلی **{pm}** را انتخاب کنید.",
           "همهٔ قابلیت‌ها همراه با GIF در [README اصلی]({main}) هستند (به زبان کره‌ای)."),
}
assert set(S) == set(ORDER), set(ORDER) ^ set(S)


def code(lang): return CODE.get(lang, lang)


def bar(here, prefix):
    """the row of languages; `prefix` leads from the page to docs/i18n, and to the main read-me with '../../'"""
    items = []
    for lang in ["ko"] + ORDER:
        name = "한국어" if lang == "ko" else S[lang][0]
        if lang == here:
            items.append(f"**{name}**")
        elif lang == "ko":
            items.append(f"[{name}]({'README.md' if prefix else '../../README.md'})")
        else:
            items.append(f"[{name}]({prefix}README.{code(lang)}.md)")
    return " · ".join(items)


def page(lang):
    g = A.G[lang]
    fmt = dict(pm=g["peace_mode"], cap=g["cap"], kind=g["kind"])
    tr = {k: v.format(**fmt) for k, v in TR[lang].items()}
    intro, feats, foot = A.text_of(lang, "yt_desc").split("\n\n")[:3]
    feats += "\n" + "\n".join("• " + tr[k] for k in ("gift", "music", "battery", "story"))
    feats = "\n".join("- " + l.lstrip("• ").strip() for l in feats.splitlines() if l.strip())
    name, h_install, s1, s2, s3, more = S[lang]
    steps = [s.format(rel=REL, main="../../README.md", pm=g["peace_mode"]) for s in (s1, s2, s3)]
    title = f"MiSide {g['peace_mode']}"
    L = [
        '<div align="center">', "",
        '<img src="../media/hero.gif" width="720" alt="MiSide Peace Mode">', "",
        f"# {title}", "",
        f"**{tr['unofficial']}**", "",
        "</div>", "",
        bar(lang, ""), "",
    ]
    body = [
        f"> **{tr['notofficial']}.** MiSide © AIHASTO.", "",
        f"**{DOWNLOAD[lang]}** — [{REPO_URL.replace('https://', '')}/releases]({REL})", "",
        intro, "",
        feats, "",
        f"## {h_install}", "",
        '<img src="../media/setup_en.png" width="520" alt="MiSide Peace Mode Setup">', "",
    ] + [f"{i}. {s}" for i, s in enumerate(steps, 1)] + [
        "",
        more.format(main="../../README.md"), "",
        "---", "",
        foot.splitlines()[0] + "  ",
        foot.splitlines()[-1],
    ]
    if lang in RTL:
        body = ['<div dir="rtl">', ""] + body + ["", "</div>"]
    return "\n".join(L + body) + "\n"


out = os.path.join(REPO, "docs", "i18n")
os.makedirs(out, exist_ok=True)
for lang in ORDER:
    open(os.path.join(out, f"README.{code(lang)}.md"), "w", encoding="utf-8", newline="\n").write(page(lang))

# the main read-me's language bar, between its markers (put under the notice the first time)
p = os.path.join(REPO, "README.md")
s = open(p, encoding="utf-8").read()
block = "<!-- languages -->\n" + bar("ko", "docs/i18n/") + "\n<!-- /languages -->"
if "<!-- languages -->" in s:
    s = re.sub(r"<!-- languages -->.*?<!-- /languages -->", lambda m: block, s, flags=re.S)
else:
    anchor = "**⬇ ["
    assert s.count(anchor) == 1
    s = s.replace(anchor, block + "\n\n" + anchor)
open(p, "w", encoding="utf-8", newline="\n").write(s)
print(len(ORDER), "pages in", out)
