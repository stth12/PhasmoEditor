using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Editor
{
    class Program
    {
        static void Main(string[] args)
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string saveDir = $@"{userProfile}\AppData\LocalLow\Kinetic Games\Phasmophobia\";
            string saveFile = $@"{userProfile}\AppData\LocalLow\Kinetic Games\Phasmophobia\SaveFile.txt";
            string saveFileDecrypted = $@"{userProfile}\AppData\LocalLow\Kinetic Games\Phasmophobia\SaveFile_Decrypted.txt";
            string saveFileEncrypted = $@"{userProfile}\AppData\LocalLow\Kinetic Games\Phasmophobia\SaveFile_Encrypted.txt";
            string backupFile = $@"{userProfile}\AppData\LocalLow\Kinetic Games\Phasmophobia\SaveFileBackup.txt";

            Console.WriteLine("       PhasmoEditor by stth12");
            Console.WriteLine(" ----------------------------------");
            Console.WriteLine("              Github");
            Console.WriteLine("    https://github.com/stth12");
            Console.WriteLine();

            Console.WriteLine("Press Enter to continue...");
            while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
            Console.WriteLine();
            Console.Clear();
            if (!File.Exists(saveFile))
            {
                Console.Clear();
                Console.WriteLine($"The Phasmophobia SaveFile doesn't exist: '{saveFile}");
                Console.WriteLine($"Press any key to exit . . .");
                Console.ReadLine();
                Environment.Exit(0);
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

            UpdateField(jsonObject, "PlayersMoney");
            UpdateField(jsonObject, "NewLevel");
            UpdateField(jsonObject, "Prestige");

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

            Console.Clear();
            Console.WriteLine("Thanx for using PhasmoEditor by stth12.");
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("              Github");
            Console.WriteLine("     https://github.com/stth12");
            Console.WriteLine();
            Console.WriteLine("          DonationAlerts");
            Console.WriteLine("https://www.donationalerts.com/r/stth12");
            Console.WriteLine();
            Console.WriteLine("         Press any key...");
            Console.ReadLine();

        }

        static void UpdateField(JObject jsonObject, string fieldName)
        {
            if (jsonObject[fieldName] != null)
            {
                Console.WriteLine();
                Console.WriteLine($"Enter a new value for {fieldName}:");
                int newValue;
                if (int.TryParse(Console.ReadLine(), out newValue))
                {
                    jsonObject[fieldName]["value"] = newValue;
                    Console.WriteLine("The value has been successfully updated!");
                }
                else
                {
                    Console.WriteLine("Incorrect input. Please, enter an integer.");
                }
            }
            else
            {
                Console.WriteLine($"The {fieldName} wasn't found in the file.");
            }
        }

        static string Decrypt(string file)
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

        static void Encrypt(string file, string data)
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
