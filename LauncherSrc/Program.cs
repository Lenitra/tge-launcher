#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600// Dereference of a possibly null reference.
#pragma warning disable CS8603// Dereference of a possibly null reference.
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.Win32;

public class Program
{



    static void Main(string[] args)
    {

        if (!IsLatestModVersionAsync().Result)
        {
            DownloadLatestModVersion();
            RunInstallUpdate();
        }

        Console.WriteLine("Lancement du launcher...");
        // Initialiser WinForms rendering settings AVANT toute création de fenêtre WinForms
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);


        bool etsStarted = false;
        KillTrucksBook();
        Thread.Sleep(500); // Attendre un peu pour s'assurer que le processus est bien terminé

        // CheckAndUpdateLauncher();

        if (!ETS2RegistryKeyExist())
        {
            Console.WriteLine("Premier lancement...");
            StartTrucksBook();
            Thread.Sleep(2000);
        }

        KillTrucksBook();
        Thread.Sleep(500); // Attendre un peu pour s'assurer que le processus est bien terminé

        Console.WriteLine("Installation du mod et configuration automatique du chemin ETS2...");
        InstallMod();
        ModifierCheminETS2(FindEuroTrucks2Exe() ?? string.Empty);


        while (CheckLogin() == false || GetCredentials() == null || GetCredentials().Email == string.Empty || GetCredentials().Password == string.Empty)
        {
            SauvegarderIdentifiants();
        }

        Console.WriteLine("Démarrage de TrucksBook...");
        StartTrucksBook();


        while (!etsStarted)
        {
            ModifierCheminETS2(FindEuroTrucks2Exe() ?? string.Empty);
            if (DetectPageOnTrucksBook() == "login")
            {
                LoginToTrucksBook();
            }

            if (DetectPageOnTrucksBook() == "home")
            {
                Thread.Sleep(500);
                StartETS2();
                etsStarted = true;
            }

        }

        Console.WriteLine("ETS2 a été lancé avec succès.");
        // Réduire la fenêtre de TrucksBook
        MinimizeTrucksBook();
    }



    #region TrucksBook 
    static void StartTrucksBook()
    {
        string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TrucksBook", "TB Client.exe");
        exePath = Path.GetFullPath(exePath);
        if (File.Exists(exePath))
        {
            try
            {
                using var proc = Process.Start(exePath);
                Console.WriteLine($"Lancement de TrucksBook depuis : {exePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du lancement de TrucksBook : {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Fichier introuvable : {exePath}");
        }
    }

    static void KillTrucksBook()
    {
        var logiciel = GetLogiciel(false);
        if (logiciel != null)
        {
            try
            {
                logiciel.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la fermeture de TrucksBook : {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Processus TrucksBook non trouvé.");
        }
    }

    static void LoginToTrucksBook()
    {
        string email = string.Empty;
        string password = string.Empty;

        var logiciel = GetLogiciel();
        if (logiciel == null)
        {
            Console.WriteLine("Processus TrucksBook non trouvé.");
            return;
        }

        // Lire les identifiants depuis credentials.json
        Credentials? creds = GetCredentials();
        if (creds == null)
        {
            Console.WriteLine("Identifiants invalides dans credentials.json.");
            return;
        }

        email = creds.Email;
        password = creds.Password;

        try
        {
            using var automation = new UIA3Automation();
            using var app = FlaUI.Core.Application.Attach(logiciel);
            var window = app.GetMainWindow(automation);
            if (window == null)
            {
                Console.WriteLine("Fenêtre principale non trouvée.");
                return;
            }

            var emailBox = window.FindFirstDescendant(cf => cf.ByAutomationId("txtEmail"))?.AsTextBox();
            var passwordBox = window.FindFirstDescendant(cf => cf.ByAutomationId("txtPass"))?.AsTextBox();
            var loginButton = window.FindFirstDescendant(cf => cf.ByAutomationId("btnLogin"))?.AsButton();

            if (emailBox == null || passwordBox == null || loginButton == null)
            {
                Console.WriteLine("Un ou plusieurs éléments de connexion sont introuvables (txtEmail, txtPass, btnLogin).");
                return;
            }

            // ValuePattern : programmatique, aucune collision possible avec l'input utilisateur.
            emailBox.Text = email;

            passwordBox.Focus();
            // PasswordBox WPF refuse ValuePattern par design (sécurité) ; on tente quand même
            // pour les contrôles qui l'autorisent, fallback sur la simulation clavier sinon.
            TrySetTextOrType(passwordBox, password);

            // InvokePattern : déclenche le bouton sans bouger la souris ni voler le focus.
            loginButton.Invoke();
            Console.WriteLine("Clic sur le bouton de connexion effectué.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la connexion: {ex.Message}");
        }
    }

    static bool CheckLogin()
    {
        // Lire les identifiants depuis credentials.json
        Credentials? creds = GetCredentials();
        if (creds == null || string.IsNullOrWhiteSpace(creds.Email) || string.IsNullOrWhiteSpace(creds.Password))
        {
            Console.WriteLine("Identifiants invalides dans credentials.json.");
            return false;
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("tge-launcher");

            var url = "https://trucksbook.eu/login-user";
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("email", creds.Email),
                new KeyValuePair<string, string>("pass", creds.Password),
            });

            using var response = client.PostAsync(url, form).GetAwaiter().GetResult();
            bool ok = response.StatusCode == HttpStatusCode.OK;
            // Afficher la réponse de la requête dans la console
            // Console.WriteLine(response.Content.ReadAsStringAsync().GetAwaiter().GetResult().Split('{"status"')[1]);
            if (ok && response.Content.ReadAsStringAsync().GetAwaiter().GetResult().Contains("\"status\":\"success\""))
            {
                return ok;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la vérification du login: {ex.Message}");
            return false;
        }
    }

    static string DetectPageOnTrucksBook()
    {
        var logiciel = GetLogiciel();
        using (var automation = new UIA3Automation())
        using (var app = FlaUI.Core.Application.Attach(logiciel))
        {
            var window = app.GetMainWindow(automation);
            if (window == null)
            {
                Console.WriteLine("Fenêtre principale non trouvée.");
                return "null";
            }

            var homeButton = window.FindFirstDescendant(cf => cf.ByAutomationId("btnSpustitEts2"))?.AsButton();
            if (homeButton != null)
            {
                return "home";
            }

            var inputLogin = window.FindFirstDescendant(cf => cf.ByAutomationId("txtPass"))?.AsTextBox();
            if (inputLogin != null)
            {
                return "login";
            }
        }
        return "autre";
    }


    static void MinimizeTrucksBook()
    {
        var logiciel = GetLogiciel();
        using (var automation = new UIA3Automation())
        using (var app = FlaUI.Core.Application.Attach(logiciel))
        {
            var window = app.GetMainWindow(automation);
            if (window == null)
            {
                Console.WriteLine("Fenêtre principale non trouvée.");
                return;
            }

            // Réduire la fenêtre
            var windowPattern = window.Patterns.Window.Pattern;
            windowPattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Minimized);
            Console.WriteLine("Fenêtre de TrucksBook réduite.");
        }
    }

    static void StartETS2()
    {
        var logiciel = GetLogiciel();
        using (var automation = new UIA3Automation())
        using (var app = FlaUI.Core.Application.Attach(logiciel))
        {
            var window = app.GetMainWindow(automation);
            if (window == null)
            {
                Console.WriteLine("Fenêtre principale non trouvée.");
                return;
            }

            var startButton = window.FindFirstDescendant(cf => cf.ByAutomationId("btnSpustitEts2"))?.AsButton();
            if (startButton == null)
            {
                Console.WriteLine("Bouton de lancement ETS2 non trouvé (AutomationId: 'btnSpustitEts2').");
                return;
            }

            // InvokePattern : pas besoin de focus ni de clic souris réel.
            startButton.Invoke();
            Console.WriteLine("Clic sur le bouton de lancement ETS2 effectué.");
        }
    }

    // Tente de poser la valeur via UIA ValuePattern (programmatique, sans collision avec l'input
    // utilisateur). En cas d'échec — typiquement une PasswordBox WPF qui bloque ValuePattern
    // pour raisons de sécurité — on retombe sur la simulation clavier de FlaUI.
    static void TrySetTextOrType(FlaUI.Core.AutomationElements.TextBox tb, string value)
    {
        try
        {
            tb.Text = value;
        }
        catch
        {
            tb.Enter(value);
        }
    }


    public static bool ETS2RegistryKeyExist()
    {
        using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\TrucksBook", writable: true);
        if (key != null)
        {
            return true;
        }
        return false;
    }

    public static void ModifierCheminETS2(string nouveauChemin)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\TrucksBook", writable: true);
            {
                if (key != null)
                {
                    key.SetValue("ets2", nouveauChemin, RegistryValueKind.String);
                    Console.WriteLine($"Chemin ETS2 modifié avec succès : {nouveauChemin}");
                }
                else
                {
                    throw new Exception("Clé de registre 'TrucksBook' introuvable.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erreur lors de la modification de la clé : " + ex.Message);
        }
    }

    #endregion



    #region Système pour récupérer le processus de TrucksBook
    static Process GetLogiciel(bool waitUntilFound = true)
    {
        string[] processNames = { "TB Client", "TrucksBook Client", "TB Client.exe" };

        Process? process = null;

        if (!waitUntilFound)
        {
            return GetProcessByNames(processNames);
        }


        while (process == null)
        {
            process = GetProcessByNames(processNames);

            Thread.Sleep(100);
            Console.WriteLine($"Tentative pour trouver le processus TrucksBook...");

        }
        return process;

    }


    static Process? GetProcessByNames(string[] names)
    {
        var lowerNames = names.Select(n => n.ToLowerInvariant()).ToHashSet();
        try
        {
            var process = Process.GetProcesses()
                .FirstOrDefault(p => lowerNames.Contains(p.ProcessName.ToLowerInvariant()));
#pragma warning disable CS8603 // Possible null reference return.
            return process;
#pragma warning restore CS8603 // Possible null reference return.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la récupération des processus : {ex.Message}");
            return null;
        }
    }

    #endregion



    #region Système pour trouver le fichier eurotrucks2.exe

    public static string? FindEuroTrucks2Exe()
    {
        // Helper: verify that the immediate parent directory name contains "x64"
        static bool IsX64Parent(string path)
        {
            var parent = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty);
            return !string.IsNullOrEmpty(parent) && parent.IndexOf("x64", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // 1) Try common Steam locations first (fast path)
        try
        {
            var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var steamRoot = Path.Combine(pf86, "Steam");

            var candidateDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddBinDirsUnder(string libraryPath)
            {
                try
                {
                    var bin = Path.Combine(libraryPath, "steamapps", "common", "Euro Truck Simulator 2", "bin");
                    if (Directory.Exists(bin))
                    {
                        foreach (var d in Directory.EnumerateDirectories(bin, "*x64*", SearchOption.TopDirectoryOnly))
                        {
                            candidateDirs.Add(d);
                        }
                    }
                }
                catch { /* ignore and continue */ }
            }

            if (Directory.Exists(steamRoot))
            {
                // Default Steam library
                AddBinDirsUnder(steamRoot);

                // Additional Steam libraries from libraryfolders.vdf
                try
                {
                    var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
                    if (File.Exists(vdf))
                    {
                        foreach (var line in File.ReadLines(vdf))
                        {
                            var m = Regex.Match(line, "\"path\"\\s+\"([^\"]+)\"");
                            if (m.Success)
                            {
                                var libPath = m.Groups[1].Value.Replace("\\\\", "\\");
                                if (Directory.Exists(libPath))
                                {
                                    AddBinDirsUnder(libPath);
                                }
                            }
                        }
                    }
                }
                catch { /* ignore parsing/IO errors */ }
            }

            foreach (var dir in candidateDirs)
            {
                var exe = Path.Combine(dir, "eurotrucks2.exe");
                if (File.Exists(exe) && IsX64Parent(exe))
                {
                    return exe;
                }
            }
        }
        catch { /* ignore and fallback to broad search */ }

        // 2) Fallback: scan all drives for directories containing "x64" and look for eurotrucks2.exe inside
        foreach (var drive in GetAllDrives())
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(drive, "*x64*", SearchOption.AllDirectories))
                {
                    var exe = Path.Combine(dir, "eurotrucks2.exe");
                    if (File.Exists(exe) && IsX64Parent(exe))
                    {
                        return exe;
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is DirectoryNotFoundException || ex is PathTooLongException)
            {
                // Ignore folders/drives we can't access
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la recherche de eurotrucks2.exe : {ex.Message}");
            }
        }

        return null;
    }


    public static List<string> GetAllDrives()
    {
        var drives = new List<string>();
        for (char drive = 'A'; drive <= 'Z'; drive++)
        {
            string driveLetter = $"{drive}:\\";
            try
            {
                if (Directory.Exists(driveLetter))
                {
                    drives.Add(driveLetter);
                }
            }
            catch { }
        }
        return drives;
    }

    #endregion


    #region Installation du mod

    public static void deleteOldMods()
    {
        string modPath = GetModPath();
        if (modPath == null)
        {
            Console.WriteLine("ERREUR : Le dossier 'mod' n'a pas été trouvé.");
            return;
        }

        // Suppression des anciens fichiers de mod
        try
        {
            // Supprimer le dossiers appelés TGE_MOD si il existe
            var oldModDirs = Directory.GetDirectories(modPath, "TGE-MOD*");
            foreach (var dir in oldModDirs)
            {
                Directory.Delete(dir, true);
                Console.WriteLine($"Ancien dossier de mod supprimé : {dir}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la suppression des anciens mods : {ex.Message}");
        }
    }

    public static void InstallMod()
    {
        deleteOldMods();

        Console.WriteLine("Installation du mod...");

        string modPath = GetModPath();
        if (modPath == null)
        {
            Console.WriteLine("ERREUR : Le dossier 'mod' n'a pas été trouvé.");
            return;
        }

        // Recherche du fichier zip du mod dans le dossier parent
        string modZipPath = Path.GetFullPath(Directory.GetFiles(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), "tge_mod_*.zip", SearchOption.TopDirectoryOnly)[0]);

        // unzip le fichier dans le dossier modPath
        try 
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(modZipPath, modPath, true); // overwrite si déjà présent
            Console.WriteLine($"Mod extrait dans : {modPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'extraction du mod : {ex.Message}");
        }

    }


    public static string? GetModPath()
    {
        // Chemin standard dans Documents
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string standardPath = Path.Combine(userProfile, "Documents", "Euro Truck Simulator 2", "mod");
        if (Directory.Exists(standardPath))
        {
            return standardPath;
        }

        // Recherche globale sur tous les disques
        foreach (var drive in GetAllDrives())
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(drive, "mod", SearchOption.AllDirectories))
                {
                    var parentDir = Path.GetFileName(Path.GetDirectoryName(dir));
                    var grandParentDir = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(dir)));
                    if (parentDir != null && parentDir.Equals("Euro Truck Simulator 2", StringComparison.OrdinalIgnoreCase))
                    {
                        return dir;
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
        }
        return null;
    }


    public static bool IsModUpToDate()
    {
        // TODO: Comparer la version du mod avec la version attendue
        // Logique pour vérifier si le mod est à jour
        return true;
    }


    #endregion





    public class Credentials
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    public static void SauvegarderIdentifiants()
    {
        // Identité visuelle TGE — palette inspirée du logo (truck bleu nuit + flammes dorées)
        var bgPrimary   = System.Drawing.Color.FromArgb(20, 24, 31);
        var bgTitleBar  = System.Drawing.Color.FromArgb(15, 18, 24);
        var bgField     = System.Drawing.Color.FromArgb(31, 37, 51);
        var accent      = System.Drawing.Color.FromArgb(245, 166, 35);
        var accentHover = System.Drawing.Color.FromArgb(255, 184, 51);
        var accentDown  = System.Drawing.Color.FromArgb(220, 145, 25);
        var textPrimary = System.Drawing.Color.FromArgb(232, 232, 232);
        var textMuted   = System.Drawing.Color.FromArgb(139, 146, 165);
        var borderField = System.Drawing.Color.FromArgb(42, 49, 66);
        var closeHover  = System.Drawing.Color.FromArgb(220, 60, 60);

        var form = new System.Windows.Forms.Form
        {
            Text = "TGE Launcher",
            ClientSize = new System.Drawing.Size(420, 540),
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None,
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
            BackColor = bgPrimary,
            TopMost = true,
        };

        // Icône de fenêtre / barre des tâches
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "logo.ico");
            if (File.Exists(iconPath))
                form.Icon = new System.Drawing.Icon(iconPath);
        }
        catch (Exception ex) { Console.WriteLine($"Icône non chargée : {ex.Message}"); }

        bool fermetureVolontaire = false;
        form.FormClosed += (s, e) =>
        {
            if (!fermetureVolontaire)
            {
                KillTrucksBook();
                Environment.Exit(0);
            }
        };

        // === Barre de titre custom ===
        var titleBar = new System.Windows.Forms.Panel
        {
            Dock = System.Windows.Forms.DockStyle.Top,
            Height = 36,
            BackColor = bgTitleBar,
        };

        var titleLabel = new System.Windows.Forms.Label
        {
            Text = "  TGE LAUNCHER",
            ForeColor = textMuted,
            Font = new System.Drawing.Font("Segoe UI", 8, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Dock = System.Windows.Forms.DockStyle.Fill,
        };
        titleBar.Controls.Add(titleLabel);

        var closeBtn = new System.Windows.Forms.Button
        {
            Text = "✕",
            Width = 46,
            Dock = System.Windows.Forms.DockStyle.Right,
            FlatStyle = System.Windows.Forms.FlatStyle.Flat,
            ForeColor = textMuted,
            BackColor = bgTitleBar,
            Font = new System.Drawing.Font("Segoe UI", 11),
            TabStop = false,
            Cursor = System.Windows.Forms.Cursors.Hand,
        };
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.FlatAppearance.MouseOverBackColor = closeHover;
        closeBtn.MouseEnter += (s, e) => closeBtn.ForeColor = System.Drawing.Color.White;
        closeBtn.MouseLeave += (s, e) => closeBtn.ForeColor = textMuted;
        closeBtn.Click += (s, e) => form.Close();
        titleBar.Controls.Add(closeBtn);

        // Drag de la fenêtre via la titlebar (HTCAPTION natif)
        System.Windows.Forms.MouseEventHandler dragHandler = (s, e) =>
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(form.Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero);
            }
        };
        titleBar.MouseDown += dragHandler;
        titleLabel.MouseDown += dragHandler;

        form.Controls.Add(titleBar);

        // === Logo ===
        var logoBox = new System.Windows.Forms.PictureBox
        {
            Width = 96,
            Height = 96,
            Top = 36 + 28,
            SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
            BackColor = bgPrimary,
        };
        logoBox.Left = (form.ClientSize.Width - logoBox.Width) / 2;
        try
        {
            var logoPath = Path.Combine(AppContext.BaseDirectory, "assets", "logo.png");
            if (File.Exists(logoPath))
                logoBox.Image = System.Drawing.Image.FromFile(logoPath);
        }
        catch (Exception ex) { Console.WriteLine($"Logo non chargé : {ex.Message}"); }
        form.Controls.Add(logoBox);

        // === Titres ===
        var lblTitle = new System.Windows.Forms.Label
        {
            Text = "TGE LAUNCHER",
            ForeColor = textPrimary,
            Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Width = form.ClientSize.Width,
            Height = 32,
            Top = logoBox.Bottom + 12,
            Left = 0,
        };
        form.Controls.Add(lblTitle);

        var lblSubtitle = new System.Windows.Forms.Label
        {
            Text = "Connectez-vous à votre compte TrucksBook",
            ForeColor = textMuted,
            Font = new System.Drawing.Font("Segoe UI", 9),
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Width = form.ClientSize.Width,
            Height = 20,
            Top = lblTitle.Bottom + 2,
            Left = 0,
        };
        form.Controls.Add(lblSubtitle);

        // === Champs ===
        int fieldWidth = 340;
        int fieldX = (form.ClientSize.Width - fieldWidth) / 2;

        var emailField = BuildField(form, "EMAIL", lblSubtitle.Bottom + 28, fieldX, fieldWidth,
                                     bgField, textPrimary, textMuted, borderField, accent);
        emailField.Field.Text = GetCredentials()?.Email ?? string.Empty;

        var pwField = BuildField(form, "MOT DE PASSE", emailField.Container.Bottom + 16, fieldX, fieldWidth,
                                  bgField, textPrimary, textMuted, borderField, accent);
        pwField.Field.UseSystemPasswordChar = true;
        pwField.Field.Text = GetCredentials()?.Password ?? string.Empty;

        // === Bouton de connexion ===
        var btnLogin = new System.Windows.Forms.Button
        {
            Text = "SE CONNECTER",
            Width = fieldWidth,
            Height = 44,
            Left = fieldX,
            Top = pwField.Container.Bottom + 24,
            FlatStyle = System.Windows.Forms.FlatStyle.Flat,
            BackColor = accent,
            ForeColor = bgPrimary,
            Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
            Cursor = System.Windows.Forms.Cursors.Hand,
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.FlatAppearance.MouseOverBackColor = accentHover;
        btnLogin.FlatAppearance.MouseDownBackColor = accentDown;
        btnLogin.Click += (sender, e) =>
        {
            string email = emailField.Field.Text.Trim();
            string password = pwField.Field.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                System.Windows.Forms.MessageBox.Show(form, "Veuillez remplir tous les champs.",
                    "Champs requis",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                System.Windows.Forms.MessageBox.Show(form, "Adresse email invalide.",
                    "Email",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            var creds = new Credentials { Email = email, Password = password };
            File.WriteAllText("credentials.json",
                JsonSerializer.Serialize(creds, new JsonSerializerOptions { WriteIndented = true }));

            fermetureVolontaire = true;
            form.Close();
        };
        form.Controls.Add(btnLogin);

        // === Lien Discord ===
        var lblHelp = new System.Windows.Forms.Label
        {
            Text = "Besoin d'aide ?  Rejoindre le Discord",
            ForeColor = textMuted,
            Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Underline),
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Width = form.ClientSize.Width,
            Height = 22,
            Top = btnLogin.Bottom + 18,
            Left = 0,
            Cursor = System.Windows.Forms.Cursors.Hand,
        };
        lblHelp.MouseEnter += (s, e) => lblHelp.ForeColor = accent;
        lblHelp.MouseLeave += (s, e) => lblHelp.ForeColor = textMuted;
        lblHelp.Click += (sender, e) =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/7yTzWATs66",
                UseShellExecute = true
            });
        };
        form.Controls.Add(lblHelp);

        // Raccourcis clavier
        form.KeyPreview = true;
        form.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Enter) btnLogin.PerformClick();
            else if (e.KeyCode == System.Windows.Forms.Keys.Escape) form.Close();
        };

        System.Windows.Forms.Application.Run(form);
    }

    private static (System.Windows.Forms.Panel Container, System.Windows.Forms.TextBox Field) BuildField(
        System.Windows.Forms.Form parent, string label, int top, int x, int width,
        System.Drawing.Color bg, System.Drawing.Color textColor, System.Drawing.Color labelColor,
        System.Drawing.Color borderColor, System.Drawing.Color accent)
    {
        var lbl = new System.Windows.Forms.Label
        {
            Text = label,
            ForeColor = labelColor,
            Font = new System.Drawing.Font("Segoe UI", 8, System.Drawing.FontStyle.Bold),
            Left = x,
            Top = top,
            Width = width,
            Height = 16,
        };
        parent.Controls.Add(lbl);

        var container = new System.Windows.Forms.Panel
        {
            Width = width,
            Height = 40,
            Left = x,
            Top = top + 18,
            BackColor = bg,
        };

        var field = new System.Windows.Forms.TextBox
        {
            BorderStyle = System.Windows.Forms.BorderStyle.None,
            BackColor = bg,
            ForeColor = textColor,
            Font = new System.Drawing.Font("Segoe UI", 11),
            Left = 12,
            Top = 12,
            Width = width - 24,
        };
        container.Controls.Add(field);

        // Trait coloré en bas (doré quand le champ a le focus, gris sinon)
        container.Paint += (s, e) =>
        {
            var color = field.Focused ? accent : borderColor;
            using var pen = new System.Drawing.Pen(color, 2);
            e.Graphics.DrawLine(pen, 0, container.Height - 1, container.Width, container.Height - 1);
        };
        field.Enter += (s, e) => container.Invalidate();
        field.Leave += (s, e) => container.Invalidate();

        parent.Controls.Add(container);
        return (container, field);
    }
    public static Credentials? GetCredentials()
    {
        if (File.Exists("credentials.json"))
        {
            string json = File.ReadAllText("credentials.json");
            return JsonSerializer.Deserialize<Credentials>(json);
        }
        return null;
    }



    public static async Task<bool> IsLatestModVersionAsync()
    {
        long githubVersion = 0;
        long localVersion = 0;


        // 1. Trouver le fichier qui a pour regex tge_mod_([0-9]+)\.zip dans le dossier actuel
        var currentFolder = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory);
        Console.WriteLine($"Recherche des fichiers de mod dans : {AppDomain.CurrentDomain.BaseDirectory}");
        // Utiliser une regex pour extraire la version depuis le nom du fichier
        foreach (var file in currentFolder)
        {
            var match = Regex.Match(Path.GetFileName(file), @"tge_mod_([0-9]+)\.zip");
            if (match.Success && long.TryParse(match.Groups[1].Value, out var ver))
            {
                if (ver > localVersion)
                    localVersion = ver;
            }
        }

        if (localVersion == 0)
        {
            Console.WriteLine("Aucun fichier de mod local trouvé.");
            return false;
        }


        // 2. Récupérer la dernière version GitHub
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("tge-launcher-check");
            var url = "https://api.github.com/repos/Lenitra/tge-launcher/releases/latest";
            var response = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var name) && name.GetString()?.StartsWith("LauncherTGE_") == true)
                    {
                        var part = name.GetString()?.Split('_')[1].Split('.')[0];
                        githubVersion = long.TryParse(part, out var ver) ? ver : 0;
                    }
                }

            }
        }
        catch
        {
            Console.WriteLine("Erreur lors de la vérification de la version GitHub.");
            return true;
        }

        // 3. Comparer
        Console.WriteLine($"{localVersion} : Local");
        Console.WriteLine($"{githubVersion} : GitHub");
        Console.WriteLine($"Comparaison des versions du launcher : {(localVersion >= githubVersion ? "À jour" : "Obsolète")}");
        return localVersion >= githubVersion;

    }


    public static void CheckAndUpdateLauncher()
    {
        if (IsLatestModVersionAsync().Result)
        {
            Console.WriteLine("Le launcher est à jour.");
            return;
        }
        Console.WriteLine("Une nouvelle version du launcher est disponible.");
        DownloadLatestModVersion();
    }


    public static void RunInstallUpdate()
    {
        string batPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.bat");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{batPath}\"", // ← "start" ouvre une nouvelle fenêtre CMD indépendante
                UseShellExecute = false,
                CreateNoWindow = false
            }
        };

        process.Start();

        // Fermer l'application principale
        Environment.Exit(0);
    }

    public static void DownloadLatestModVersion()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("tge-launcher");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var url = "https://api.github.com/repos/Lenitra/tge-launcher/releases/latest";
            var response = client.GetStringAsync(url).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(response);
            string? downloadUrl = null;
            string? assetName = null;
            if (doc.RootElement.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var name) && name.GetString()?.StartsWith("LauncherTGE_") == true &&
                        asset.TryGetProperty("browser_download_url", out var download))
                    {
                        downloadUrl = download.GetString();
                        assetName = name.GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                Console.WriteLine("Aucun asset 'LauncherTGE_*' trouvé sur la release GitHub.");
                return;
            }

            // Déterminer le nom de fichier de destination
            string fileName;
            try
            {
                var uri = new Uri(downloadUrl);
                fileName = Path.GetFileName(uri.LocalPath);
            }
            catch
            {
                fileName = assetName ?? "LauncherTGE_latest.zip";
            }

            // Enregistrer dans un sous-dossier "build" proche de l'exécutable
            var buildDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "build");
            Directory.CreateDirectory(buildDir);
            var destinationPath = Path.Combine(buildDir, fileName);

            Console.WriteLine($"Téléchargement de '{fileName}'...");
            using var resp = client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            resp.EnsureSuccessStatusCode();
            using var stream = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.CopyTo(fs);
            fs.Flush(true);
            Console.WriteLine($"Fichier téléchargé: {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du téléchargement de la dernière version du mod : {ex.Message}");
        }
    }



    #region Utility Methods
    public static void ListInteractiveElementsWithFlaUI()
    {
        try
        {
            using var automation = new UIA3Automation();
            using var app = FlaUI.Core.Application.Attach(GetLogiciel());

            var window = app.GetMainWindow(automation);
            if (window == null)
            {
                Console.WriteLine("Fenêtre principale non trouvée.");
                return;
            }

            Console.WriteLine($"Fenêtre principale détectée : {window.Title}\n");
            Console.WriteLine("Exploration des éléments interactifs de la fenêtre :\n");

            ListElementsRecursive(window, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erreur lors de l'exploration de la fenêtre : " + ex.Message);
        }
    }

    private static void ListElementsRecursive(AutomationElement element, int level)
    {
        string indent = new string(' ', level * 2);
        string name = element.Name;
        string controlType = element.ControlType.ToString() ?? "?";
        string automationId = element.AutomationId;

        Console.WriteLine($"{indent}- [{controlType}] Name: '{name}'  Id: '{automationId}'");

        var children = element.FindAllChildren();
        foreach (var child in children)
        {
            ListElementsRecursive(child, level + 1);
        }
    }



    public static void DebugElement(AutomationElement element)
    {
        if (element == null)
        {
            Console.WriteLine("[DebugElement] Element est null.");
            return;
        }
        Console.WriteLine("[DebugElement] --- Détail de l'élément ---");
        Console.WriteLine($"Type: {element.ControlType}");
        Console.WriteLine($"Name: '{element.Name}'");
        Console.WriteLine($"AutomationId: '{element.AutomationId}'");
        Console.WriteLine($"ClassName: '{element.ClassName}'");
        Console.WriteLine($"FrameworkId: '{element.FrameworkType}'");
        Console.WriteLine($"BoundingRectangle: {element.BoundingRectangle}");
        Console.WriteLine($"IsEnabled: {element.IsEnabled}");
        Console.WriteLine($"IsOffscreen: {element.IsOffscreen}");
        Console.WriteLine("[DebugElement] --------------------------");
    }


    #endregion


}
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Dereference of a possibly null reference.
#pragma warning disable CS8600// Dereference of a possibly null reference.