// MiSide Peace Mode - the installer (a small Windows program for players).
// Finds the game, puts BepInEx in if it is missing (downloaded from BepInEx's own release page), copies the mod and
// its launcher into the game folder, and can start the game. Built with the C# compiler that ships with Windows
// (.NET Framework 4, C# 5: see build.py), so it runs on any Windows 10/11 without installing anything.
// The mod's DLL and the launcher .bat are embedded as resources.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PeaceModeSetup
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 2 && args[0] == "/shots") { MainForm.Shots(args[1]); return; }   // (for the read-me: a picture of the window per language)
            // without the window: /install <game folder>, /uninstall <game folder>  (exit code 0 = done, 1 = failed, 2 = no game there, 3 = the game is running)
            if (args.Length == 2 && (args[0] == "/install" || args[0] == "/uninstall")) { Environment.Exit(MainForm.Silent(args[0] == "/install", args[1])); return; }
            Application.Run(new MainForm());
        }
    }

    // every text of the window, by language (the key is the first column of Texts.All)
    static class Texts
    {
        public static readonly string[] Codes = { "ko", "en", "ja", "zh-Hans", "zh-Hant", "ru", "es", "pt", "de", "fr" };
        public static readonly string[] Names = { "한국어", "English", "日本語", "简体中文", "繁體中文", "Русский", "Español", "Português", "Deutsch", "Français" };

        // key, then one text per language in the order of Codes
        static readonly string[][] All =
        {
            new[] { "title", "MiSide Peace Mode 설치", "MiSide Peace Mode Setup", "MiSide Peace Mode インストーラー", "MiSide Peace Mode 安装程序", "MiSide Peace Mode 安裝程式", "Установка MiSide Peace Mode", "Instalador de MiSide Peace Mode", "Instalador do MiSide Peace Mode", "MiSide Peace Mode – Installation", "Installation de MiSide Peace Mode" },
            new[] { "sub", "비공식 팬 모드입니다. AIHASTO의 공식 평화 모드가 아니며 AIHASTO와 무관합니다.", "Unofficial fan mod. Not AIHASTO's official Peaceful Mode and not affiliated with AIHASTO.", "非公式ファンMODです。AIHASTO公式のピースフル・モードではなく、AIHASTOとは無関係です。", "非官方粉丝模组。不是 AIHASTO 官方的和平模式，与 AIHASTO 无关。", "非官方粉絲模組。不是 AIHASTO 官方的和平模式，與 AIHASTO 無關。", "Неофициальный фанатский мод. Это не официальный «Мирный режим» от AIHASTO и не связан с AIHASTO.", "Mod no oficial hecho por fans. No es el Modo Pacífico oficial de AIHASTO ni está afiliado a AIHASTO.", "Mod não oficial feito por fãs. Não é o Modo Pacífico oficial da AIHASTO nem tem ligação com a AIHASTO.", "Inoffizielle Fan-Mod. Nicht der offizielle Friedliche Modus von AIHASTO und nicht mit AIHASTO verbunden.", "Mod de fan non officiel. Ce n'est pas le Mode Pacifique officiel d'AIHASTO et il n'est pas affilié à AIHASTO." },
            new[] { "lang", "언어", "Language", "言語", "语言", "語言", "Язык", "Idioma", "Idioma", "Sprache", "Langue" },
            new[] { "folder", "MiSide 게임 폴더 (MiSideFull.exe가 있는 곳)", "MiSide game folder (where MiSideFull.exe is)", "MiSide のゲームフォルダー（MiSideFull.exe がある場所）", "MiSide 游戏文件夹（MiSideFull.exe 所在位置）", "MiSide 遊戲資料夾（MiSideFull.exe 所在位置）", "Папка игры MiSide (где лежит MiSideFull.exe)", "Carpeta del juego MiSide (donde está MiSideFull.exe)", "Pasta do jogo MiSide (onde está MiSideFull.exe)", "MiSide-Spielordner (in dem MiSideFull.exe liegt)", "Dossier du jeu MiSide (où se trouve MiSideFull.exe)" },
            new[] { "browse", "찾아보기…", "Browse…", "参照…", "浏览…", "瀏覽…", "Обзор…", "Examinar…", "Procurar…", "Durchsuchen…", "Parcourir…" },
            new[] { "found", "✔ MiSideFull.exe 확인됨", "✔ MiSideFull.exe found", "✔ MiSideFull.exe を確認しました", "✔ 已找到 MiSideFull.exe", "✔ 已找到 MiSideFull.exe", "✔ MiSideFull.exe найден", "✔ MiSideFull.exe encontrado", "✔ MiSideFull.exe encontrado", "✔ MiSideFull.exe gefunden", "✔ MiSideFull.exe trouvé" },
            new[] { "notfound", "✖ 이 폴더에 MiSideFull.exe가 없습니다", "✖ MiSideFull.exe is not in this folder", "✖ このフォルダーに MiSideFull.exe がありません", "✖ 此文件夹中没有 MiSideFull.exe", "✖ 此資料夾中沒有 MiSideFull.exe", "✖ В этой папке нет MiSideFull.exe", "✖ MiSideFull.exe no está en esta carpeta", "✖ MiSideFull.exe não está nesta pasta", "✖ MiSideFull.exe ist nicht in diesem Ordner", "✖ MiSideFull.exe n'est pas dans ce dossier" },
            new[] { "modin", "설치된 모드: {0}", "Installed mod: {0}", "インストール済みのMOD: {0}", "已安装的模组：{0}", "已安裝的模組：{0}", "Установленный мод: {0}", "Mod instalado: {0}", "Mod instalado: {0}", "Installierte Mod: {0}", "Mod installé : {0}" },
            new[] { "shortcut", "바탕 화면에 바로 가기 만들기", "Create a desktop shortcut", "デスクトップにショートカットを作成", "创建桌面快捷方式", "建立桌面捷徑", "Создать ярлык на рабочем столе", "Crear un acceso directo en el escritorio", "Criar um atalho na área de trabalho", "Desktopverknüpfung erstellen", "Créer un raccourci sur le bureau" },
            new[] { "install", "설치", "Install", "インストール", "安装", "安裝", "Установить", "Instalar", "Instalar", "Installieren", "Installer" },
            new[] { "uninstall", "모드 제거", "Remove the mod", "MODを削除", "移除模组", "移除模組", "Удалить мод", "Quitar el mod", "Remover o mod", "Mod entfernen", "Supprimer le mod" },
            new[] { "launch", "게임 실행", "Start the game", "ゲームを起動", "启动游戏", "啟動遊戲", "Запустить игру", "Iniciar el juego", "Iniciar o jogo", "Spiel starten", "Lancer le jeu" },
            new[] { "close", "닫기", "Close", "閉じる", "关闭", "關閉", "Закрыть", "Cerrar", "Fechar", "Schließen", "Fermer" },
            new[] { "ready", "준비됨. '설치'를 누르세요.", "Ready. Press Install.", "準備完了。「インストール」を押してください。", "已就绪。请点击“安装”。", "已就緒。請按「安裝」。", "Готово. Нажмите «Установить».", "Listo. Pulsa Instalar.", "Pronto. Clique em Instalar.", "Bereit. Klicke auf Installieren.", "Prêt. Cliquez sur Installer." },
            new[] { "bephave", "BepInEx가 이미 설치되어 있습니다.", "BepInEx is already installed.", "BepInEx は既にインストールされています。", "BepInEx 已安装。", "BepInEx 已安裝。", "BepInEx уже установлен.", "BepInEx ya está instalado.", "BepInEx já está instalado.", "BepInEx ist bereits installiert.", "BepInEx est déjà installé." },
            new[] { "bepdl", "BepInEx(모드 로더) 내려받는 중… {0}%", "Downloading BepInEx (the mod loader)… {0}%", "BepInEx（MODローダー）をダウンロード中… {0}%", "正在下载 BepInEx（模组加载器）… {0}%", "正在下載 BepInEx（模組載入器）… {0}%", "Загрузка BepInEx (загрузчик модов)… {0}%", "Descargando BepInEx (el cargador de mods)… {0}%", "Baixando o BepInEx (carregador de mods)… {0}%", "BepInEx (Mod-Loader) wird heruntergeladen… {0}%", "Téléchargement de BepInEx (le chargeur de mods)… {0}%" },
            new[] { "bepzip", "BepInEx 압축 푸는 중…", "Extracting BepInEx…", "BepInEx を展開中…", "正在解压 BepInEx…", "正在解壓縮 BepInEx…", "Распаковка BepInEx…", "Extrayendo BepInEx…", "Extraindo o BepInEx…", "BepInEx wird entpackt…", "Extraction de BepInEx…" },
            new[] { "copy", "모드 파일 넣는 중…", "Copying the mod…", "MODファイルをコピー中…", "正在复制模组文件…", "正在複製模組檔案…", "Копирование файлов мода…", "Copiando el mod…", "Copiando o mod…", "Mod-Dateien werden kopiert…", "Copie du mod…" },
            new[] { "done", "설치 완료! '게임 실행'을 누르세요. 처음 실행은 몇 분 걸립니다(검은 창이 떠 있는 동안 기다려 주세요).", "Installed! Press Start the game. The first start takes a few minutes (wait while the console window works).", "インストール完了！「ゲームを起動」を押してください。初回起動は数分かかります（コンソールが動いている間お待ちください）。", "安装完成！请点击“启动游戏”。首次启动需要几分钟（控制台窗口运行期间请耐心等待）。", "安裝完成！請按「啟動遊戲」。首次啟動需要幾分鐘（主控台視窗執行期間請耐心等候）。", "Установлено! Нажмите «Запустить игру». Первый запуск занимает несколько минут (подождите, пока работает окно консоли).", "¡Instalado! Pulsa Iniciar el juego. El primer inicio tarda unos minutos (espera mientras trabaja la ventana de consola).", "Instalado! Clique em Iniciar o jogo. A primeira inicialização leva alguns minutos (aguarde enquanto a janela do console trabalha).", "Installiert! Klicke auf Spiel starten. Der erste Start dauert einige Minuten (warte, solange das Konsolenfenster arbeitet).", "Installé ! Cliquez sur Lancer le jeu. Le premier démarrage prend quelques minutes (patientez pendant que la console travaille)." },
            new[] { "removed", "모드를 제거했습니다. BepInEx와 세이브는 그대로 둡니다.", "The mod was removed. BepInEx and your saves were left as they are.", "MODを削除しました。BepInEx とセーブデータはそのままです。", "已移除模组。BepInEx 和存档保持不变。", "已移除模組。BepInEx 和存檔保持不變。", "Мод удалён. BepInEx и сохранения оставлены как есть.", "Se quitó el mod. BepInEx y tus partidas guardadas se dejaron como estaban.", "O mod foi removido. O BepInEx e seus saves foram mantidos.", "Die Mod wurde entfernt. BepInEx und deine Spielstände bleiben erhalten.", "Le mod a été supprimé. BepInEx et vos sauvegardes sont conservés." },
            new[] { "running", "게임이 실행 중입니다. 게임을 끄고 다시 눌러 주세요.", "The game is running. Close it and try again.", "ゲームが実行中です。終了してからもう一度お試しください。", "游戏正在运行。请关闭游戏后重试。", "遊戲正在執行。請關閉遊戲後再試一次。", "Игра запущена. Закройте её и попробуйте снова.", "El juego está en ejecución. Ciérralo e inténtalo de nuevo.", "O jogo está em execução. Feche-o e tente novamente.", "Das Spiel läuft. Schließe es und versuche es erneut.", "Le jeu est en cours d'exécution. Fermez-le et réessayez." },
            new[] { "errdl", "BepInEx를 내려받지 못했습니다. 인터넷 연결을 확인해 주세요.", "BepInEx could not be downloaded. Check your internet connection.", "BepInEx をダウンロードできませんでした。インターネット接続を確認してください。", "无法下载 BepInEx。请检查网络连接。", "無法下載 BepInEx。請檢查網路連線。", "Не удалось загрузить BepInEx. Проверьте подключение к интернету.", "No se pudo descargar BepInEx. Comprueba tu conexión a internet.", "Não foi possível baixar o BepInEx. Verifique sua conexão com a internet.", "BepInEx konnte nicht heruntergeladen werden. Prüfe deine Internetverbindung.", "Impossible de télécharger BepInEx. Vérifiez votre connexion internet." },
            new[] { "err", "오류: {0}", "Error: {0}", "エラー: {0}", "错误：{0}", "錯誤：{0}", "Ошибка: {0}", "Error: {0}", "Erro: {0}", "Fehler: {0}", "Erreur : {0}" },
            new[] { "note", "설치 후에는 '게임 실행' 버튼, 바탕 화면 바로 가기, 또는 게임 폴더의 'MiSide Peace Mode.bat'으로 켜세요. Steam에서 바로 켜면 모드가 뜨지 않을 수 있습니다.", "After installing, start the game with the button here, the desktop shortcut, or 'MiSide Peace Mode.bat' in the game folder. Started straight from Steam, the mod may not load.", "インストール後は、このボタン、デスクトップのショートカット、またはゲームフォルダーの「MiSide Peace Mode.bat」で起動してください。Steam から直接起動するとMODが読み込まれないことがあります。", "安装后，请使用此处的按钮、桌面快捷方式或游戏文件夹中的“MiSide Peace Mode.bat”启动游戏。直接从 Steam 启动时模组可能不会加载。", "安裝後，請使用此處的按鈕、桌面捷徑或遊戲資料夾中的「MiSide Peace Mode.bat」啟動遊戲。直接從 Steam 啟動時模組可能不會載入。", "После установки запускайте игру этой кнопкой, ярлыком на рабочем столе или файлом «MiSide Peace Mode.bat» в папке игры. При запуске прямо из Steam мод может не загрузиться.", "Después de instalar, inicia el juego con este botón, el acceso directo del escritorio o 'MiSide Peace Mode.bat' en la carpeta del juego. Si lo inicias directamente desde Steam, puede que el mod no se cargue.", "Depois de instalar, inicie o jogo por este botão, pelo atalho da área de trabalho ou por 'MiSide Peace Mode.bat' na pasta do jogo. Iniciado direto pela Steam, o mod pode não carregar.", "Starte das Spiel nach der Installation über diesen Knopf, die Desktopverknüpfung oder 'MiSide Peace Mode.bat' im Spielordner. Direkt über Steam gestartet wird die Mod eventuell nicht geladen.", "Après l'installation, lancez le jeu avec ce bouton, le raccourci du bureau ou « MiSide Peace Mode.bat » dans le dossier du jeu. Lancé directement depuis Steam, le mod peut ne pas se charger." },
            new[] { "pick", "MiSideFull.exe가 있는 폴더를 고르세요", "Choose the folder that contains MiSideFull.exe", "MiSideFull.exe があるフォルダーを選んでください", "请选择包含 MiSideFull.exe 的文件夹", "請選擇包含 MiSideFull.exe 的資料夾", "Выберите папку, в которой лежит MiSideFull.exe", "Elige la carpeta que contiene MiSideFull.exe", "Escolha a pasta que contém MiSideFull.exe", "Wähle den Ordner, der MiSideFull.exe enthält", "Choisissez le dossier qui contient MiSideFull.exe" },
        };

        static readonly Dictionary<string, string[]> map = Build();
        public static int Lang;

        static Dictionary<string, string[]> Build()
        {
            var d = new Dictionary<string, string[]>();
            foreach (var row in All) d[row[0]] = row;
            return d;
        }

        public static string T(string key)
        {
            string[] row;
            if (!map.TryGetValue(key, out row)) return key;
            int i = Lang + 1;
            return i < row.Length ? row[i] : row[2];
        }

        public static string F(string key, object arg) { return string.Format(T(key), arg); }

        // the language Windows is shown in
        public static int Detect()
        {
            var c = CultureInfo.CurrentUICulture;
            string two = c.TwoLetterISOLanguageName, name = c.Name;
            if (two == "ko") return 0;
            if (two == "ja") return 2;
            if (two == "zh") return name.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0 || name.EndsWith("TW") || name.EndsWith("HK") || name.EndsWith("MO") ? 4 : 3;
            if (two == "ru" || two == "uk" || two == "be" || two == "kk") return 5;
            if (two == "es") return 6;
            if (two == "pt") return 7;
            if (two == "de") return 8;
            if (two == "fr") return 9;
            return 1;
        }
    }

    class MainForm : Form
    {
        const string BepInExUrl = "https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip";
        const string PluginName = "MiSidePeaceMode.dll";
        const string LauncherName = "MiSide Peace Mode.bat";

        readonly Label lblTitle = new Label(), lblSub = new Label(), lblFolder = new Label(), lblFound = new Label(), lblStatus = new Label(), lblNote = new Label(), lblVersion = new Label();
        readonly ComboBox cmbLang = new ComboBox();
        readonly TextBox txtFolder = new TextBox();
        readonly Button btnBrowse = new Button(), btnInstall = new Button(), btnRemove = new Button(), btnLaunch = new Button(), btnClose = new Button();
        readonly CheckBox chkShortcut = new CheckBox();
        readonly ProgressBar bar = new ProgressBar();
        string statusKey = "ready";
        object statusArg;
        bool working;

        public MainForm()
        {
            // (laid out by hand for the screen's scale: left to WinForms' own scaling, some of the labels were scaled twice)
            AutoScaleMode = AutoScaleMode.None;
            float k;
            using (var g = CreateGraphics()) k = g.DpiX / 96f;
            Action<Control, int, int, int, int> B = (c, x, y, w, h) => c.SetBounds((int)(x * k), (int)(y * k), (int)(w * k), (int)(h * k));
            Font = new Font("Segoe UI", 9.5f);
            ClientSize = new Size((int)(640 * k), (int)(440 * k));
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(255, 246, 250);

            B(lblTitle, 22, 16, 420, 34);
            lblTitle.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(196, 60, 150);
            B(lblVersion, 22, 50, 300, 20);
            lblVersion.ForeColor = Color.FromArgb(120, 100, 120);
            B(cmbLang, 490, 18, 130, 26);
            cmbLang.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLang.Items.AddRange(Texts.Names);

            B(lblSub, 22, 76, 598, 44);
            lblSub.ForeColor = Color.FromArgb(90, 60, 20);
            lblSub.BackColor = Color.FromArgb(255, 236, 170);
            lblSub.Padding = new Padding((int)(8 * k), 0, (int)(8 * k), 0);
            lblSub.TextAlign = ContentAlignment.MiddleLeft;

            B(lblFolder, 22, 134, 598, 20);
            B(txtFolder, 22, 156, 486, 26);
            B(btnBrowse, 516, 154, 104, 29);
            B(lblFound, 22, 186, 598, 20);

            B(chkShortcut, 24, 214, 590, 24);
            chkShortcut.Checked = true;

            B(bar, 22, 246, 598, 16);
            B(lblStatus, 22, 268, 598, 42);
            B(lblNote, 22, 314, 598, 62);
            lblNote.Font = new Font("Segoe UI", 8.75f);
            lblNote.ForeColor = Color.FromArgb(120, 100, 120);

            B(btnInstall, 22, 390, 150, 36);
            btnInstall.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnInstall.BackColor = Color.FromArgb(222, 80, 170);
            btnInstall.ForeColor = Color.White;
            btnInstall.FlatStyle = FlatStyle.Flat;
            btnInstall.FlatAppearance.BorderSize = 0;
            B(btnLaunch, 182, 390, 150, 36);
            B(btnRemove, 342, 390, 150, 36);
            B(btnClose, 502, 390, 118, 36);

            Controls.AddRange(new Control[] { lblTitle, lblVersion, cmbLang, lblSub, lblFolder, txtFolder, btnBrowse, lblFound, chkShortcut, bar, lblStatus, lblNote, btnInstall, btnLaunch, btnRemove, btnClose });
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            cmbLang.SelectedIndexChanged += delegate { Texts.Lang = cmbLang.SelectedIndex; ApplyTexts(); };
            txtFolder.TextChanged += delegate { CheckFolder(); };
            btnBrowse.Click += delegate { Browse(); };
            btnInstall.Click += delegate { StartWork(true); };
            btnRemove.Click += delegate { StartWork(false); };
            btnLaunch.Click += delegate { Launch(); };
            btnClose.Click += delegate { Close(); };
            FormClosing += delegate(object s, FormClosingEventArgs e) { if (working) e.Cancel = true; };

            Texts.Lang = Texts.Detect();
            cmbLang.SelectedIndex = Texts.Lang;
            txtFolder.Text = FindGame() ?? "";
            ApplyTexts();
        }

        static bool quiet;
        protected override bool ShowWithoutActivation { get { return quiet; } }

        public static void Shots(string dir)
        {
            quiet = true;
            Directory.CreateDirectory(dir);
            using (var f = new MainForm())
            {
                f.Show();
                for (int i = 0; i < Texts.Codes.Length; i++)
                {
                    f.cmbLang.SelectedIndex = i;
                    Application.DoEvents();
                    using (var b = new Bitmap(f.Width, f.Height))
                    {
                        f.DrawToBitmap(b, new Rectangle(0, 0, f.Width, f.Height));
                        var o = f.PointToScreen(Point.Empty);
                        var inside = new Rectangle(o.X - f.Left, o.Y - f.Top, f.ClientSize.Width, f.ClientSize.Height);
                        using (var c = b.Clone(inside, b.PixelFormat))
                            c.Save(Path.Combine(dir, "setup_" + Texts.Codes[i] + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
        }

        static string ModVersion()
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v.Major + "." + v.Minor + "." + v.Build;
        }

        void ApplyTexts()
        {
            Text = Texts.T("title");
            lblTitle.Text = "MiSide Peace Mode";
            lblVersion.Text = "v" + ModVersion() + "  ·  MiSide 0.93L  ·  BepInEx 6 (IL2CPP)";
            lblSub.Text = Texts.T("sub");
            lblFolder.Text = Texts.T("folder");
            btnBrowse.Text = Texts.T("browse");
            chkShortcut.Text = Texts.T("shortcut");
            btnInstall.Text = Texts.T("install");
            btnRemove.Text = Texts.T("uninstall");
            btnLaunch.Text = Texts.T("launch");
            btnClose.Text = Texts.T("close");
            lblNote.Text = Texts.T("note");
            ShowStatus();
            CheckFolder();
        }

        void ShowStatus() { lblStatus.Text = statusArg != null ? Texts.F(statusKey, statusArg) : Texts.T(statusKey); }

        void SetStatus(string key, object arg, int percent)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string, object, int>(SetStatus), key, arg, percent); return; }
            statusKey = key; statusArg = arg;
            ShowStatus();
            if (percent >= 0) bar.Value = Math.Max(0, Math.Min(100, percent));
        }

        string GameDir { get { return txtFolder.Text.Trim().Trim('"'); } }
        bool GameOk { get { try { return GameDir.Length > 0 && File.Exists(Path.Combine(GameDir, "MiSideFull.exe")); } catch { return false; } } }
        string PluginPath { get { return Path.Combine(GameDir, "BepInEx", "plugins", PluginName); } }

        void CheckFolder()
        {
            bool ok = GameOk;
            string text = Texts.T(ok ? "found" : "notfound");
            bool installed = false;
            try
            {
                if (ok && File.Exists(PluginPath))
                {
                    installed = true;
                    var fv = FileVersionInfo.GetVersionInfo(PluginPath);
                    string v = string.IsNullOrEmpty(fv.ProductVersion) ? "?" : fv.ProductVersion.Split('+')[0];
                    text += "   ·   " + Texts.F("modin", v);
                }
            }
            catch { }
            lblFound.Text = text;
            lblFound.ForeColor = ok ? Color.FromArgb(30, 130, 70) : Color.FromArgb(190, 50, 50);
            btnInstall.Enabled = ok && !working;
            btnRemove.Enabled = ok && installed && !working;
            btnLaunch.Enabled = ok && installed && !working;
            btnBrowse.Enabled = txtFolder.Enabled = cmbLang.Enabled = !working;
        }

        void Browse()
        {
            using (var d = new FolderBrowserDialog())
            {
                d.Description = Texts.T("pick");
                d.ShowNewFolderButton = false;
                try { if (Directory.Exists(GameDir)) d.SelectedPath = GameDir; } catch { }
                if (d.ShowDialog(this) == DialogResult.OK) txtFolder.Text = d.SelectedPath;
            }
        }

        // the game folder: next to this program, then every Steam library
        static string FindGame()
        {
            var cands = new List<string>();
            try
            {
                string here = Path.GetDirectoryName(Application.ExecutablePath);
                cands.Add(here);
                cands.Add(Path.GetDirectoryName(here));
            }
            catch { }
            foreach (var steam in SteamRoots())
            {
                cands.Add(Path.Combine(steam, "steamapps", "common", "MiSide"));
                try
                {
                    string vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
                    if (File.Exists(vdf))
                        foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                            cands.Add(Path.Combine(m.Groups[1].Value.Replace("\\\\", "\\"), "steamapps", "common", "MiSide"));
                }
                catch { }
            }
            foreach (var c in cands)
            {
                try { if (!string.IsNullOrEmpty(c) && File.Exists(Path.Combine(c, "MiSideFull.exe"))) return Path.GetFullPath(c); } catch { }
            }
            return null;
        }

        static IEnumerable<string> SteamRoots()
        {
            var list = new List<string>();
            string[][] keys =
            {
                new[] { "HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath" },
                new[] { "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam", "InstallPath" },
                new[] { "HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath" },
            };
            foreach (var k in keys)
            {
                try
                {
                    var v = Registry.GetValue(k[0], k[1], null) as string;
                    if (!string.IsNullOrEmpty(v)) list.Add(v.Replace('/', '\\'));
                }
                catch { }
            }
            return list;
        }

        // is the game of this folder running? (its files cannot be replaced then)
        static bool GameRunning(string dir)
        {
            try
            {
                string exe = Path.GetFullPath(Path.Combine(dir, "MiSideFull.exe"));
                foreach (var p in Process.GetProcessesByName("MiSideFull"))
                {
                    try { if (string.Equals(Path.GetFullPath(p.MainModule.FileName), exe, StringComparison.OrdinalIgnoreCase)) return true; }
                    catch { return true; }   // (cannot tell which copy it is: take it as this one)
                }
            }
            catch { }
            return false;
        }

        public static int Silent(bool install, string dir)
        {
            try
            {
                dir = Path.GetFullPath(dir.Trim().Trim('"'));
                if (!File.Exists(Path.Combine(dir, "MiSideFull.exe"))) return 2;
                if (GameRunning(dir)) return 3;
                using (var f = new MainForm())
                {
                    if (install) f.DoInstall(dir, false); else f.DoRemove(dir);
                }
                return 0;
            }
            catch { return 1; }
        }

        void StartWork(bool install)
        {
            if (working || !GameOk) return;
            if (GameRunning(GameDir)) { SetStatus("running", null, 0); return; }
            working = true;
            CheckFolder();
            string dir = GameDir;
            bool shortcut = chkShortcut.Checked;
            var t = new Thread(delegate()
            {
                try
                {
                    if (install) DoInstall(dir, shortcut); else DoRemove(dir);
                }
                catch (WebException) { SetStatus("errdl", null, 0); }
                catch (Exception e) { SetStatus("err", e.Message, 0); }
                BeginInvoke(new Action(delegate { working = false; CheckFolder(); }));
            });
            t.IsBackground = true;
            t.Start();
        }

        void DoInstall(string dir, bool shortcut)
        {
            // 1. the mod loader, unless it is there
            if (File.Exists(Path.Combine(dir, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll")) && File.Exists(Path.Combine(dir, "winhttp.dll")))
            {
                SetStatus("bephave", null, 60);
            }
            else
            {
                string local = LocalBepInEx();
                if (local != null)
                {
                    SetStatus("bepzip", null, 62);
                    Extract(local, dir);
                }
                else
                {
                    string zip = Path.Combine(Path.GetTempPath(), "BepInEx-MiSidePeaceMode-" + Guid.NewGuid().ToString("N") + ".zip");
                    try
                    {
                        Download(BepInExUrl, zip);
                        SetStatus("bepzip", null, 62);
                        Extract(zip, dir);
                    }
                    finally { try { File.Delete(zip); } catch { } }
                }
                if (!File.Exists(Path.Combine(dir, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll")) || !File.Exists(Path.Combine(dir, "winhttp.dll")))
                    throw new InvalidDataException("BepInEx-Unity.IL2CPP-win-x64");   // (the wrong zip: not the IL2CPP x64 build)
            }
            // 2. the mod and its launcher
            SetStatus("copy", null, 85);
            Directory.CreateDirectory(Path.Combine(dir, "BepInEx", "plugins"));
            WriteResource(PluginName, Path.Combine(dir, "BepInEx", "plugins", PluginName));
            WriteResource("launcher.bat", Path.Combine(dir, LauncherName));
            if (shortcut) { try { MakeShortcut(dir); } catch { } }
            SetStatus("done", null, 100);
        }

        // a BepInEx zip put next to this program (BepInEx-Unity.IL2CPP-win-x64-*.zip)
        static string LocalBepInEx()
        {
            try
            {
                var found = Directory.GetFiles(Path.GetDirectoryName(Application.ExecutablePath), "BepInEx-Unity.IL2CPP-win-x64-*.zip");
                return found.Length > 0 ? found[0] : null;
            }
            catch { return null; }
        }

        void DoRemove(string dir)
        {
            foreach (var f in new[] { Path.Combine(dir, "BepInEx", "plugins", PluginName), Path.Combine(dir, LauncherName),
                                      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "MiSide Peace Mode.lnk") })
            {
                try { if (File.Exists(f)) File.Delete(f); } catch { }
            }
            SetStatus("removed", null, 0);
        }

        void Download(string url, string path)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;   // TLS 1.2
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = "MiSidePeaceMode-Setup";
            req.Timeout = 30000;
            req.ReadWriteTimeout = 60000;
            using (var res = (HttpWebResponse)req.GetResponse())
            using (var src = res.GetResponseStream())
            using (var dst = File.Create(path))
            {
                long total = res.ContentLength, got = 0;
                var buf = new byte[81920];
                int n, last = -1;
                while ((n = src.Read(buf, 0, buf.Length)) > 0)
                {
                    dst.Write(buf, 0, n);
                    got += n;
                    int pct = total > 0 ? (int)(got * 100 / total) : 0;
                    if (pct != last) { last = pct; SetStatus("bepdl", pct, pct * 60 / 100); }
                }
                if (total > 0 && got != total) throw new WebException("incomplete download");
            }
        }

        static void Extract(string zip, string dir)
        {
            string root = Path.GetFullPath(dir).TrimEnd('\\') + "\\";
            using (var z = ZipFile.OpenRead(zip))
            {
                foreach (var e in z.Entries)
                {
                    string target = Path.GetFullPath(Path.Combine(dir, e.FullName));
                    if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;   // (nothing outside the game folder)
                    if (e.FullName.EndsWith("/") || e.FullName.EndsWith("\\")) { Directory.CreateDirectory(target); continue; }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    e.ExtractToFile(target, true);
                }
            }
        }

        static void WriteResource(string name, string path)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null) throw new FileNotFoundException(name);
                using (var f = File.Create(path)) s.CopyTo(f);
            }
        }

        // a desktop shortcut to the launcher (through the Windows Script Host: no extra references)
        static void MakeShortcut(string dir)
        {
            string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "MiSide Peace Mode.lnk");
            var type = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(type);
            object sc = type.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnk });
            var st = sc.GetType();
            st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { Path.Combine(dir, LauncherName) });
            st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { dir });
            st.InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc, new object[] { Path.Combine(dir, "MiSideFull.exe") + ",0" });
            st.InvokeMember("WindowStyle", BindingFlags.SetProperty, null, sc, new object[] { 7 });
            st.InvokeMember("Description", BindingFlags.SetProperty, null, sc, new object[] { "MiSide Peace Mode (unofficial fan mod)" });
            st.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
        }

        void Launch()
        {
            try
            {
                string bat = Path.Combine(GameDir, LauncherName);
                if (!File.Exists(bat)) return;
                var psi = new ProcessStartInfo(bat);
                psi.WorkingDirectory = GameDir;
                psi.WindowStyle = ProcessWindowStyle.Minimized;
                Process.Start(psi);
            }
            catch (Exception e) { SetStatus("err", e.Message, -1); }
        }
    }
}
