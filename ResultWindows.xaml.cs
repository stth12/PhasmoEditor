using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace PhasmoEditor
{
    public partial class ResultWindow : Window
    {
        private string playersMoney;
        private string newLevel;
        private string prestige;

        public ResultWindow(string playersMoney, string newLevel, string prestige)
        {
            InitializeComponent();
            this.playersMoney = playersMoney;
            this.newLevel = newLevel;
            this.prestige = prestige;
            RunMainCode();
        }

        private void RunMainCode()
        {
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string saveDir = $@"{userProfile}\AppData\LocalLow\Kinetic Games\Phasmophobia\";
                string saveFile = $@"{userProfile}\AppData\LocalLow\Kinetic Games\Phasmophobia\SaveFile.txt";
                string saveFileDecrypted = $@"{userProfile}\AppData\LocalLow\Kinetic Games\Phasmophobia\SaveFile_Decrypted.txt";
                string saveFileEncrypted = $@"{userProfile}\AppData\LocalLow\Kinetic Games\Phasmophobia\SaveFile_Encrypted.txt";
                string backupFile = $@"{userProfile}\AppData\LocalLow\Kinetic Games\Phasmophobia\SaveFileBackup.txt";

                if (!File.Exists(saveFile))
                {
                    throw new FileNotFoundException($"The Phasmophobia SaveFile doesn't exist: '{saveFile}'");
                }

                if (File.Exists(backupFile))
                    File.Delete(backupFile);
                File.Copy(saveFile, backupFile);

                if (File.Exists(saveFileDecrypted))
                    File.Delete(saveFileDecrypted);

                string decryptedText = Decrypt(saveFile);
                File.WriteAllText(saveFileDecrypted, decryptedText);

                string filePath = $@"{userProfile}\AppData\LocalLow\Kinetic Games\Phasmophobia\SaveFile_Decrypted.txt";

                string fileContent = File.ReadAllText(filePath);

                JObject jsonObject = JObject.Parse(fileContent);

                UpdateField(jsonObject, "PlayersMoney", playersMoney);
                UpdateField(jsonObject, "NewLevel", newLevel);
                UpdateField(jsonObject, "Prestige", prestige);

                string updatedContent = jsonObject.ToString();
                File.WriteAllText(filePath, updatedContent);

                string data = File.ReadAllText(saveFileDecrypted);
                Encrypt(saveFileEncrypted, data);

                if (File.Exists(backupFile))
                    File.Delete(backupFile);
                File.Copy(saveFile, backupFile);

                File.Delete(saveFile);
                File.Delete(saveFileDecrypted);

                File.Move(saveFileEncrypted, saveFile);

                ResultTextBlock.Text = "Success!";
            }
            catch (Exception ex)
            {
                ResultTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        private void UpdateField(JObject jsonObject, string fieldName, string newValue)
        {
            if (jsonObject[fieldName] != null)
            {
                if (int.TryParse(newValue, out int intValue))
                {
                    jsonObject[fieldName]["value"] = intValue;
                }
                else
                {
                    throw new FormatException($"Invalid input for {fieldName}. Please enter an integer.");
                }
            }
            else
            {
                throw new ArgumentException($"The {fieldName} wasn't found in the file.");
            }
        }

        private string Decrypt(string file)
        {
            byte[] data = File.ReadAllBytes(file);
            byte[] password = Encoding.ASCII.GetBytes("t36gref9u84y7f43g");

            byte[] iv = new byte[16];
            Buffer.BlockCopy(data, 0, iv, 0, 16);

            using (var aes = new AesManaged())
            {
                aes.Key = new Rfc2898DeriveBytes(password, iv, 100).GetBytes(16);
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;

                using (var decryptor = aes.CreateDecryptor())
                using (var inputStream = new MemoryStream(data, 16, data.Length - 16))
                using (var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream))
                {
                    string decryptedData = reader.ReadToEnd();
                    while (decryptedData[decryptedData.Length - 1] != '}')
                    {
                        decryptedData = decryptedData.Substring(0, decryptedData.Length - 1);
                    }
                    return decryptedData;
                }
            }
        }

        private void Encrypt(string file, string data)
        {
            data = data.Replace("'", "\"").Replace("True", "true").Replace("False", "false");

            byte[] password = Encoding.UTF8.GetBytes("t36gref9u84y7f43g");

            byte[] iv = new byte[16];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(iv);
            }

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = new Rfc2898DeriveBytes(password, iv, 100).GetBytes(16);
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    msEncrypt.Write(iv, 0, iv.Length);
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(data);
                        }
                    }

                    byte[] encryptedData = msEncrypt.ToArray();

                    File.WriteAllBytes(file, encryptedData);
                }
            }
        }
    }
}
