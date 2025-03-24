using System.Text;
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;



namespace SecureDataSaver
{
    public static class DataSaver
    {
        private const string PASSWORD = "OH;;r$yl[oW@TH'EXe^pwxeSdy.[aNE;"; //set any string of 32 chars
        private static readonly string IV = "9019072968381112"; //set any string of 16 chars
        public static string SelectedPath
        {
            get 
            {   if(!isInitialized)
                    InitializationDataSaver();
                    
                return selectedPath;
            }
            private set 
            {
                selectedPath = value;
            }
        }
        private static string selectedPath;
        public static readonly string externalPath = Application.persistentDataPath + "/_db/";
        private static bool isInitialized;

        #region Common Section
        public static void InitializationDataSaver()
        {
            SetDataPath(externalPath);
        }

        public static void SetDataPath(string storageLocation)
        {
            if (isInitialized)
                return;

            if (!storageLocation.EndsWith("/"))
                storageLocation += "/";

            selectedPath = storageLocation;

            if (!Directory.Exists(selectedPath))
                Directory.CreateDirectory(selectedPath);

            isInitialized = true;
        }

        public static bool CheckIfPathExist(string path)
        {
            return File.Exists(path);
        }

        #endregion Common Section

        #region Data Encrypt/Decrypt Section
        public static string AESEncryptor(string dataToEncrypt, string key, string iv)
        {
            try
            {
                byte[] dataArray = null;
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.IV = Encoding.UTF8.GetBytes(iv);

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                            {
                                streamWriter.Write(dataToEncrypt);
                            }

                            dataArray = memoryStream.ToArray();
                        }
                    }
                }

                return Convert.ToBase64String(dataArray);
            }
            catch (Exception ex)
            {
                Debug.Log("Got Error while encrypting: " + ex.Message);
                return string.Empty;
            }
        }

        public static string AESDecrypt(string cipherText, string key, string iv)
        {
            try
            {
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.IV = Encoding.UTF8.GetBytes(iv);
                    ICryptoTransform decrypt = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decrypt, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Got Error While Decrypting AES String: " + ex.Message);
                return string.Empty;
            }
        }
        #endregion Data Encrypt/Decrypt Section

        #region Data Read/Write Section

        public static bool WriteData<T>(T sourceData, string fileName)
        {
            if (sourceData == null) //Checking if data is empty or null
                return false;

            string dataToWrite = JsonUtility.ToJson(sourceData);

            Debug.Log(dataToWrite);

            StreamWriter streamWriter = new StreamWriter(SelectedPath + fileName, false);
            try
            {
                streamWriter.Write(dataToWrite);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log("Can't Write the Data: " + ex.Message);
                return false;
            }
            finally
            {
                streamWriter.Flush();
                streamWriter.Close();
            }
        }

        public static bool WriteData(string dataToWrite, string fileName)
        {
            if (string.IsNullOrEmpty(dataToWrite)) //Checking if data is empty or null
                return false;

            StreamWriter streamWriter = new StreamWriter(SelectedPath + fileName, false);
            try
            {
                streamWriter.Write(dataToWrite);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log("Can't Write the Data: " + ex.Message);
                return false;
            }
            finally
            {
                streamWriter.Flush();
                streamWriter.Close();
            }
        }

        public static void WriteAllBytes(byte[] dataToWrite, string fileName)
        {
            if (dataToWrite == null || dataToWrite.Length == 0)
            {
                Debug.LogError("Data is null or empty. Skipping write.");
                return;
            }

            // Replace invalid characters
            string safeFileName = fileName.Replace("@", "_").Replace(".", "_");
            string path = Path.Combine(SelectedPath, safeFileName);

            // Ensure directory exists
            if (!Directory.Exists(SelectedPath))
            {
                Directory.CreateDirectory(SelectedPath);
            }

            Debug.LogError($"Path: {path}");

            try
            {
                File.WriteAllBytes(path, dataToWrite);
                Debug.Log("File saved successfully at: " + path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed To Save Data to: " + path.Replace("/", "\\"));
                Debug.LogWarning("Error: " + e.Message);
            }
        }

        public static string ReadData(string fileName)
        {
            string path = string.Concat(SelectedPath, fileName);
            if (CheckIfPathExist(path))
            {
                StreamReader reader = new StreamReader(path);
                try
                {
                    //Debug.Log("ReadData: " + reader.ReadToEnd());
                    return reader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    Debug.Log("Got Error while reading Data: " + ex.Message);
                    return string.Empty;
                }
                finally
                {
                    reader.Close();
                }
            }

            return string.Empty;
        }

        public static T ReadData<T>(string fileName)
        {
            string path = string.Concat(SelectedPath, fileName);

            if (CheckIfPathExist(path))
            {
                StreamReader reader = new StreamReader(path);
                try
                {
                    //Debug.Log("Data Read: " + reader.ReadToEnd());
                    return JsonUtility.FromJson<T>(reader.ReadToEnd());
                }
                catch (Exception ex)
                {
                    Debug.Log("Got Error while reading Data: " + ex.Message);
                    return default(T);
                }
                finally
                {
                    Debug.Log("From Finally in Read");
                    reader.Close();
                }
            }

            return default(T);
        }
        
        
        public static byte[] ReadImageData(string fileName)
        {
            byte[] dataByte = null;
            string path = string.Concat(SelectedPath, fileName);

            //Exit if Directory or File does not exist
            if (!CheckIfPathExist(path))
            {
                Debug.LogError($"Image Path does not exist, Path: {path}");
                return null;
            }
            
            try
            {
                //Debug.LogWarning("Directory does not exist");
                dataByte = File.ReadAllBytes(path);

                return dataByte;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed To Load Data from: " + path.Replace("/", "\\"));
                Debug.LogWarning("Error: " + e.Message);

                return dataByte;
            }
        }


        public static bool AESEncryptAndWriteData(string dataToEncrypt, string fileName,
            string password = null, string iv = null)
        {
            if (string.IsNullOrEmpty(dataToEncrypt))
                return false;

            password ??= PASSWORD;
            iv ??= IV;

            return WriteData(AESEncryptor(dataToEncrypt, password, iv), fileName);
        }

        public static string AESDecryptAndReadData(string fileName, string password = null, string iv = null)
        {
            string dataToDecrypt = ReadData(fileName);

            if (string.IsNullOrEmpty(dataToDecrypt))
                return string.Empty;

            password ??= PASSWORD;
            iv ??= IV;

            return AESDecrypt(dataToDecrypt, password, iv);
        }

        public static bool AESEncryptAndWriteData<T>(T dataToEncrypt, string fileName, string password = null, string iv = null)
        {
            password ??= PASSWORD;
            iv ??= IV;

            string jsonStr = JsonUtility.ToJson(dataToEncrypt);

            if (string.IsNullOrEmpty(jsonStr))
                return false;

            return WriteData(AESEncryptor(jsonStr, password, iv), fileName);
        }

        public static T AESDecryptAndReadData<T>(string fileName, string password = null, string iv = null)
        {
            string dataToDecrypt = ReadData(fileName);

            if (string.IsNullOrEmpty(dataToDecrypt))
                return default(T);

            password ??= PASSWORD;
            iv ??= IV;

            string decryptedData = AESDecrypt(dataToDecrypt, password, iv);
            return JsonUtility.FromJson<T>(decryptedData);
        }
        

        #endregion Data Read/Write Section

        #region Delete Data

        public static void DeleteFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("DeleteFile called with an empty or null fileName.");
                return;
            }

            string path = Path.Combine(SelectedPath, fileName);

            Debug.Log($"Trying to delete file: {path}");

            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"File deleted successfully: {path}");
            }
            else
            {
                Debug.LogError($"File Doesn't exist on path: {path}");
            }
        }

        #endregion / Delete Data
        
        #region Secure PlayerPrefs
        public static void SetSecureStringPrefs(string prefsKey, string value)
        {
            string encryptedString = AESEncryptor(value, PASSWORD, IV);
            PlayerPrefs.SetString(prefsKey, encryptedString);
        }

        public static string GetSecureStringPrefs(string prefsKey)
        {
            return AESDecrypt(PlayerPrefs.GetString(prefsKey), PASSWORD, IV);
        }
        #endregion Secure PlayerPrefs
    }
}
