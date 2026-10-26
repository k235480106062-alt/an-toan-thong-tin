using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        // Biến lưu trữ AES
        private byte[] _aesKey;
        private byte[] _aesIV;

        // Biến lưu trữ RSA
        private RSA _rsa;
        private byte[] _lastEncryptedBytes;
        private byte[] _originalData;

        // Controls giao diện AES
        private TextBox txtAesKey;
        private TextBox txtAesPlainText;
        private TextBox txtAesCipherText;
        private Label lblAesStatus;

        // Controls giao diện RSA & SHA
        private RichTextBox rtbPemViewer;
        private TextBox txtShaInput;
        private TextBox txtShaOutput;
        private Label lblRsaStatus;

        public Form1()
        {
            InitializeComponent();
            SetupCustomUI();
        }

        private void SetupCustomUI()
        {
            this.Text = "Chuong trinh Thuc hanh Ma hoa AES & RSA - SHA256";
            this.Size = new Size(950, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            TabControl tabCtrl = new TabControl { Dock = DockStyle.Fill };
            TabPage tabAes = new TabPage("Bai 1: Ma hoa AES (Doi xung)");
            TabPage tabRsa = new TabPage("Bai 2: Ma hoa RSA & SHA-256 (Bat doi xung)");

            // ================= GIAO DIỆN BÀI 1: AES =================
            int y = 15;
            Button btnGenKey = new Button { Text = "1. Sinh khoa AES", Location = new Point(20, y), Size = new Size(160, 32) };
            btnGenKey.Click += BtnGenAesKey_Click;
            Button btnSaveKey = new Button { Text = "2. Luu khoa ra file", Location = new Point(190, y), Size = new Size(160, 32) };
            btnSaveKey.Click += BtnSaveAesKey_Click;
            Button btnLoadKey = new Button { Text = "3. Import khoa tu file", Location = new Point(360, y), Size = new Size(160, 32) };
            btnLoadKey.Click += BtnLoadAesKey_Click;
            tabAes.Controls.AddRange(new Control[] { btnGenKey, btnSaveKey, btnLoadKey });

            y += 40;
            tabAes.Controls.Add(new Label { Text = "Thong tin Khoa & IV (Base64):", Location = new Point(20, y), AutoSize = true });
            y += 20;
            txtAesKey = new TextBox { Location = new Point(20, y), Size = new Size(880, 25), ReadOnly = true };
            tabAes.Controls.Add(txtAesKey);

            y += 35;
            tabAes.Controls.Add(new Label { Text = "4. Ma hoa van ban - Nhap Plaintext:", Location = new Point(20, y), AutoSize = true });
            y += 20;
            txtAesPlainText = new TextBox { Location = new Point(20, y), Size = new Size(430, 80), Multiline = true, Text = "An toan va bao mat thong tin 2026" };
            Button btnEncText = new Button { Text = "Ma hoa Text >>", Location = new Point(460, y + 25), Size = new Size(120, 30) };
            btnEncText.Click += BtnEncText_Click;
            txtAesCipherText = new TextBox { Location = new Point(590, y), Size = new Size(310, 80), Multiline = true, ReadOnly = true };
            tabAes.Controls.AddRange(new Control[] { txtAesPlainText, btnEncText, txtAesCipherText });

            y += 95;
            tabAes.Controls.Add(new Label { Text = "5. Ma hoa file lon (Hang GB) & Danh gia toc do:", Location = new Point(20, y), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) });
            y += 25;
            Button btnEncFile = new Button { Text = "Ma hoa File (Stream)", Location = new Point(20, y), Size = new Size(180, 35) };
            btnEncFile.Click += BtnEncFile_Click;
            Button btnDecFile = new Button { Text = "Giai ma File (Stream)", Location = new Point(210, y), Size = new Size(180, 35) };
            btnDecFile.Click += BtnDecFile_Click;
            Button btnCompare = new Button { Text = "Compare Bytes (2 File)", Location = new Point(400, y), Size = new Size(180, 35) };
            btnCompare.Click += BtnCompare_Click;
            tabAes.Controls.AddRange(new Control[] { btnEncFile, btnDecFile, btnCompare });

            y += 45;
            lblAesStatus = new Label { Text = "Trang thai: San sang thuc hien.", Location = new Point(20, y), Size = new Size(880, 80), ForeColor = Color.Blue };
            tabAes.Controls.Add(lblAesStatus);

            // ================= GIAO DIỆN BÀI 2: RSA & SHA =================
            int ry = 15;
            Button btnRsaGen = new Button { Text = "1. Sinh cap khoa RSA", Location = new Point(20, ry), Size = new Size(170, 30) };
            btnRsaGen.Click += BtnRsaGen_Click;
            Button btnRsaExport = new Button { Text = "2. Xuat PEM ra file", Location = new Point(200, ry), Size = new Size(170, 30) };
            btnRsaExport.Click += BtnRsaExport_Click;
            Button btnImportPub = new Button { Text = "4a. Import Public PEM (n, e)", Location = new Point(380, ry), Size = new Size(200, 30) };
            btnImportPub.Click += BtnImportPub_Click;
            Button btnImportPriv = new Button { Text = "4b. Import Private PEM (d...)", Location = new Point(590, ry), Size = new Size(200, 30) };
            btnImportPriv.Click += BtnImportPriv_Click;
            tabRsa.Controls.AddRange(new Control[] { btnRsaGen, btnRsaExport, btnImportPub, btnImportPriv });

            ry += 38;
            tabRsa.Controls.Add(new Label { Text = "3. Thong tin cau truc file PEM & cac tham so (n, e, d):", Location = new Point(20, ry), AutoSize = true });
            ry += 20;
            rtbPemViewer = new RichTextBox { Location = new Point(20, ry), Size = new Size(880, 150) };
            tabRsa.Controls.Add(rtbPemViewer);

            ry += 160;
            Button btnRsaEnc = new Button { Text = "5. Ma hoa 32-byte AES key", Location = new Point(20, ry), Size = new Size(200, 32) };
            btnRsaEnc.Click += BtnRsaEnc_Click;
            Button btnRsaDec = new Button { Text = "6. Giai ma & So sanh", Location = new Point(230, ry), Size = new Size(180, 32) };
            btnRsaDec.Click += BtnRsaDec_Click;
            Button btnBench = new Button { Text = "7. Do toc do Ma hoa vs Giai ma", Location = new Point(420, ry), Size = new Size(220, 32) };
            btnBench.Click += BtnBench_Click;
            tabRsa.Controls.AddRange(new Control[] { btnRsaEnc, btnRsaDec, btnBench });

            ry += 42;
            tabRsa.Controls.Add(new Label { Text = "8. Ham bam SHA-256 (Go text de xem hieu ung tuyet lo):", Location = new Point(20, ry), AutoSize = true });
            ry += 20;
            txtShaInput = new TextBox { Location = new Point(20, ry), Size = new Size(400, 25), Text = "Hello World" };
            txtShaInput.TextChanged += TxtShaInput_TextChanged;
            txtShaOutput = new TextBox { Location = new Point(430, ry), Size = new Size(470, 25), ReadOnly = true };
            tabRsa.Controls.AddRange(new Control[] { txtShaInput, txtShaOutput });

            ry += 35;
            Button btnSignPss = new Button { Text = "9. De xuat Chu ky so ngau nhien (RSASSA-PSS)", Location = new Point(20, ry), Size = new Size(350, 35), BackColor = Color.LightGreen };
            btnSignPss.Click += BtnSignPss_Click;
            tabRsa.Controls.Add(btnSignPss);

            ry += 42;
            lblRsaStatus = new Label { Text = "Ket qua thuc hien RSA se hien thi o day.", Location = new Point(20, ry), Size = new Size(880, 70), ForeColor = Color.DarkGreen };
            tabRsa.Controls.Add(lblRsaStatus);

            tabCtrl.TabPages.Add(tabAes);
            tabCtrl.TabPages.Add(tabRsa);
            this.Controls.Add(tabCtrl);

            ComputeSha256();
        }

        // ==========================================
        // PHẦN 1: BÀI TẬP AES
        // ==========================================

        private void BtnGenAesKey_Click(object sender, EventArgs e)
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();
                _aesKey = aes.Key;
                _aesIV = aes.IV;
            }
            txtAesKey.Text = $"Key: {Convert.ToBase64String(_aesKey)} | IV: {Convert.ToBase64String(_aesIV)}";
            lblAesStatus.Text = "Da sinh khoa AES 256-bit moi.";
        }

        private void BtnSaveAesKey_Click(object sender, EventArgs e)
        {
            if (_aesKey == null) { MessageBox.Show("Vui long sinh khoa truoc!"); return; }
            SaveFileDialog sfd = new SaveFileDialog { Filter = "Key file (*.key)|*.key", FileName = "aes_key.key" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using BinaryWriter bw = new BinaryWriter(File.Open(sfd.FileName, FileMode.Create));
                bw.Write(_aesKey.Length);
                bw.Write(_aesKey);
                bw.Write(_aesIV.Length);
                bw.Write(_aesIV);
                lblAesStatus.Text = $"Khoa da luu vao: {sfd.FileName}";
            }
        }

        private void BtnLoadAesKey_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Key file (*.key)|*.key" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                using BinaryReader br = new BinaryReader(File.Open(ofd.FileName, FileMode.Open));
                int kLen = br.ReadInt32();
                _aesKey = br.ReadBytes(kLen);
                int ivLen = br.ReadInt32();
                _aesIV = br.ReadBytes(ivLen);
                txtAesKey.Text = $"Key: {Convert.ToBase64String(_aesKey)} | IV: {Convert.ToBase64String(_aesIV)}";
                lblAesStatus.Text = "Import khoa AES thanh cong.";
            }
        }

        private void BtnEncText_Click(object sender, EventArgs e)
        {
            if (_aesKey == null) { MessageBox.Show("Chua co khoa AES! Hay bam nut '1. Sinh khoa AES' truoc."); return; }
            using Aes aes = Aes.Create();
            aes.Key = _aesKey;
            aes.IV = _aesIV;
            using MemoryStream ms = new MemoryStream();
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (StreamWriter sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(txtAesPlainText.Text);
            }
            txtAesCipherText.Text = Convert.ToBase64String(ms.ToArray());
        }

        private void BtnEncFile_Click(object sender, EventArgs e)
        {
            if (_aesKey == null) { MessageBox.Show("Chua co khoa AES! Hay bam nut '1. Sinh khoa AES' truoc."); return; }
            OpenFileDialog ofd = new OpenFileDialog { Title = "Chon file can ma hoa (co the chon file hang GB)" };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Luu file ma hoa (*.enc)",
                Filter = "Encrypted (*.enc)|*.enc",
                FileName = Path.GetFileName(ofd.FileName) + ".enc"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            Stopwatch sw = Stopwatch.StartNew();
            using (Aes aes = Aes.Create())
            {
                aes.Key = _aesKey;
                aes.IV = _aesIV;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                using FileStream fsIn = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                using FileStream fsOut = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                using ICryptoTransform encryptor = aes.CreateEncryptor();
                using CryptoStream cs = new CryptoStream(fsOut, encryptor, CryptoStreamMode.Write);

                fsIn.CopyTo(cs);
                cs.FlushFinalBlock();
            }
            sw.Stop();
            long sizeMb = new FileInfo(ofd.FileName).Length / (1024 * 1024);
            lblAesStatus.Text = $"Ma hoa hoan tat! Kich thuoc: {sizeMb} MB | Thoi gian: {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2} giay)";
            MessageBox.Show("Ma hoa file thanh cong!", "Thong bao");
        }

        private void BtnDecFile_Click(object sender, EventArgs e)
        {
            if (_aesKey == null) { MessageBox.Show("Chua co khoa AES!"); return; }
            OpenFileDialog ofd = new OpenFileDialog { Title = "Chon file da ma hoa (*.enc)", Filter = "Encrypted (*.enc)|*.enc|All files (*.*)|*.*" };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            string origName = Path.GetFileNameWithoutExtension(ofd.FileName);
            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Luu file sau khi giai ma",
                FileName = "decrypted_" + origName
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _aesKey;
                    aes.IV = _aesIV;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Mode = CipherMode.CBC;

                    using FileStream fsIn = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using FileStream fsOut = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                    using ICryptoTransform decryptor = aes.CreateDecryptor();
                    using CryptoStream cs = new CryptoStream(fsIn, decryptor, CryptoStreamMode.Read);

                    cs.CopyTo(fsOut);
                }
                sw.Stop();
                lblAesStatus.Text = $"Giai ma hoan tat! Thoi gian: {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2} giay)";
                MessageBox.Show("Giai ma file thanh cong!", "Thong bao");
            }
            catch (CryptographicException)
            {
                MessageBox.Show("Loi: Khoa AES hoac IV hien tai khong dung voi file ma hoa nay!", "Loi giai ma", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCompare_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd1 = new OpenFileDialog { Title = "BUOC 1: Chon file GOC ban dau" };
            if (ofd1.ShowDialog() != DialogResult.OK) return;

            OpenFileDialog ofd2 = new OpenFileDialog { Title = "BUOC 2: Chon file SAU KHI GIAI MA (decrypted_...)" };
            if (ofd2.ShowDialog() != DialogResult.OK) return;

            using FileStream fs1 = new FileStream(ofd1.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using FileStream fs2 = new FileStream(ofd2.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (fs1.Length != fs2.Length)
            {
                lblAesStatus.Text = $"Ket qua so sanh: THAT BAI! Kich thuoc lech: File 1 ({fs1.Length} bytes) vs File 2 ({fs2.Length} bytes).";
                MessageBox.Show("Hai file co dung luong khac nhau! Hay chac chan ban chon dung file goc va file sau giai ma (khong chon file .enc).", "Ket qua", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            byte[] b1 = new byte[1024 * 1024];
            byte[] b2 = new byte[1024 * 1024];
            int c1;
            bool match = true;
            while ((c1 = fs1.Read(b1, 0, b1.Length)) > 0)
            {
                fs2.Read(b2, 0, b2.Length);
                for (int i = 0; i < c1; i++)
                {
                    if (b1[i] != b2[i]) { match = false; break; }
                }
                if (!match) break;
            }

            if (match)
            {
                lblAesStatus.Text = "Ket qua so sanh: THANH CONG! Hai file trung khop 100% tung byte du lieu.";
                MessageBox.Show("Chuc mung! Hai file trung khop 100% tung byte.", "Ket qua", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblAesStatus.Text = "Ket qua so sanh: THAT BAI (Noi dung byte khong khop).";
                MessageBox.Show("Du lieu ben trong hai file khong dong nhat!", "Ket qua", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // PHẦN 2: BÀI TẬP RSA & SHA-256
        // ==========================================

        private void BtnRsaGen_Click(object sender, EventArgs e)
        {
            _rsa = RSA.Create(2048);
            rtbPemViewer.Text = "Da sinh cap khoa RSA 2048-bit moi.\r\nBan co the bam xuat PEM hoac tien hanh ma hoa ngay.";
        }

        private void BtnRsaExport_Click(object sender, EventArgs e)
        {
            if (_rsa == null) _rsa = RSA.Create(2048);
            string pubPem = _rsa.ExportRSAPublicKeyPem();
            string privPem = _rsa.ExportRSAPrivateKeyPem();
            File.WriteAllText("public_key.pem", pubPem);
            File.WriteAllText("private_key.pem", privPem);
            rtbPemViewer.Text = $"=== PUBLIC KEY (public_key.pem) ===\r\n{pubPem}\r\n=== PRIVATE KEY (private_key.pem) ===\r\n{privPem}";
            lblRsaStatus.Text = "Da xuat thanh cong 2 file: public_key.pem va private_key.pem vao thu muc ung dung.";
        }

        private void BtnImportPub_Click(object sender, EventArgs e)
        {
            string fileName = "public_key.pem";
            if (!File.Exists(fileName))
            {
                MessageBox.Show("Chua tim thay file public_key.pem! Hay bam nut '2. Xuat PEM ra file' truoc.", "Thong bao");
                return;
            }

            _rsa = RSA.Create();
            _rsa.ImportFromPem(File.ReadAllText(fileName));
            RSAParameters p = _rsa.ExportParameters(false);
            rtbPemViewer.Text = $"[PUBLIC KEY DER STRUCTURE]\r\n" +
                                $"- Modulus (n) [{p.Modulus.Length} bytes]: {Convert.ToHexString(p.Modulus)}\r\n\r\n" +
                                $"- Exponent (e) [{p.Exponent.Length} bytes]: {Convert.ToHexString(p.Exponent)} (e = 65537 - so mu nho giup ma hoa sieu toc)";
            lblRsaStatus.Text = "Da import thanh cong public_key.pem.";
        }

        private void BtnImportPriv_Click(object sender, EventArgs e)
        {
            string fileName = "private_key.pem";
            if (!File.Exists(fileName))
            {
                MessageBox.Show("Chua tim thay file private_key.pem! Hay bam nut '2. Xuat PEM ra file' truoc.", "Thong bao");
                return;
            }

            _rsa = RSA.Create();
            _rsa.ImportFromPem(File.ReadAllText(fileName));
            RSAParameters p = _rsa.ExportParameters(true);
            rtbPemViewer.Text = $"[PRIVATE KEY DER STRUCTURE]\r\n" +
                                $"- Modulus (n): {Convert.ToHexString(p.Modulus).Substring(0, 40)}...\r\n" +
                                $"- Exponent (e): {Convert.ToHexString(p.Exponent)}\r\n" +
                                $"- Private Exponent (d) [{p.D.Length} bytes - so mu rat lon]:\r\n{Convert.ToHexString(p.D)}";
            lblRsaStatus.Text = "Da import thanh cong private_key.pem.";
        }

        private void BtnRsaEnc_Click(object sender, EventArgs e)
        {
            if (_rsa == null) _rsa = RSA.Create(2048);
            _originalData = RandomNumberGenerator.GetBytes(32); // Gia lap ma hoa khoa AES 32-byte
            _lastEncryptedBytes = _rsa.Encrypt(_originalData, RSAEncryptionPadding.OaepSHA256);
            string b64 = Convert.ToBase64String(_lastEncryptedBytes);
            File.WriteAllText("rsa_encrypted.txt", b64);
            lblRsaStatus.Text = $"Da ma hoa 32-byte AES key bang RSA-OAEP!\r\nDu lieu goc (Hex): {Convert.ToHexString(_originalData)}\r\nDa luu ra file rsa_encrypted.txt (Base64).";
        }

        private void BtnRsaDec_Click(object sender, EventArgs e)
        {
            if (_rsa == null || !File.Exists("rsa_encrypted.txt")) { MessageBox.Show("Chua co khoa RSA hoac chua co file rsa_encrypted.txt!"); return; }
            byte[] cipher = Convert.FromBase64String(File.ReadAllText("rsa_encrypted.txt"));
            byte[] decrypted = _rsa.Decrypt(cipher, RSAEncryptionPadding.OaepSHA256);
            bool match = _originalData != null && _originalData.SequenceEqual(decrypted);
            lblRsaStatus.Text = $"Giai ma thanh cong (Hex): {Convert.ToHexString(decrypted)}\r\nSo sanh voi ban goc: {(match ? "TRUNG KHOP 100%" : "KHONG TRUNG KHOP")}";
        }

        private void BtnBench_Click(object sender, EventArgs e)
        {
            if (_rsa == null) _rsa = RSA.Create(2048);
            byte[] test = RandomNumberGenerator.GetBytes(32);
            int count = 500;

            Stopwatch swEnc = Stopwatch.StartNew();
            for (int i = 0; i < count; i++) _rsa.Encrypt(test, RSAEncryptionPadding.OaepSHA256);
            swEnc.Stop();

            byte[] cipher = _rsa.Encrypt(test, RSAEncryptionPadding.OaepSHA256);
            Stopwatch swDec = Stopwatch.StartNew();
            for (int i = 0; i < count; i++) _rsa.Decrypt(cipher, RSAEncryptionPadding.OaepSHA256);
            swDec.Stop();

            lblRsaStatus.Text = $"Thu nghiem {count} lan:\r\n- Ma hoa (Public Key e=65537 nho): {swEnc.ElapsedMilliseconds} ms\r\n- Giai ma (Private Key d 2048-bit lon): {swDec.ElapsedMilliseconds} ms\r\n=> Giai ma cham hon ma hoa ~{(double)swDec.ElapsedMilliseconds / swEnc.ElapsedMilliseconds:F1} lan!";
        }

        private void TxtShaInput_TextChanged(object sender, EventArgs e) => ComputeSha256();

        private void ComputeSha256()
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(txtShaInput.Text ?? ""));
            txtShaOutput.Text = Convert.ToHexString(hash);
        }

        private void BtnSignPss_Click(object sender, EventArgs e)
        {
            if (_rsa == null) _rsa = RSA.Create(2048);
            byte[] data = Encoding.UTF8.GetBytes(txtShaInput.Text);

            // Thuc hien ky 2 lan tren cung mot du lieu va cung mot private key
            byte[] sig1 = _rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            byte[] sig2 = _rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

            bool areEqual = sig1.SequenceEqual(sig2);
            bool v1 = _rsa.VerifyData(data, sig1, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            bool v2 = _rsa.VerifyData(data, sig2, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

            rtbPemViewer.Text = $"[KIEM CHUNG CHU KY SO NGAU NHIEN - RSASSA-PSS]\r\n" +
                                $"- Chu ky lan 1: {Convert.ToBase64String(sig1).Substring(0, 50)}...\r\n" +
                                $"- Chu ky lan 2: {Convert.ToBase64String(sig2).Substring(0, 50)}...\r\n" +
                                $"- Hai chu ky co giong nhau khong? -> {areEqual} (Khac nhau hoan toan do co Salt ngau nhien!)\r\n" +
                                $"- Verify lan 1: {v1} | Verify lan 2: {v2} (Ca hai chu ky deu hop le voi cung mot Public Key)";
        }
    }
}