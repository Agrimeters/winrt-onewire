using com.dalsemi.onewire.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Windows.Storage;

namespace com.dalsemi.onewire
{
    public class Properties
    {
        /// <summary>
        /// property table
        /// </summary>
        private Dictionary<string, string> props = null;
 
        /// <summary>
        /// Default constructor
        /// </summary>
        public Properties()
        {
        }

        /// <summary>
        /// Routine to populate the hash table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="reader"></param>
        private static void loadTable(Dictionary<string, string> table, StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] st = line.Split(new char[] { '=' });

                if (st.Length < 2)
                    continue;
                else if (st.Length == 2)
                {
                    if (st[0].StartsWith("#"))
                    {
                        Debug.WriteLine("Commented out property >> " + st[0] + "=" + st[1]);
                        continue;
                    }

                    table.Add(st[0].Trim(), st[1].Trim());
                }
                else if (st.Length > 2)
                {
                    Debug.WriteLine("Property ignored as it has more than one '='!");
                    continue;
                }
            };
        }

        /// <summary>
        /// Called to load property file from local folder on filesystem
        /// </summary>
        /// <param name="file"></param>
        public async void loadLocalFile(string file)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            if (File.Exists(localFolder.Path + "\\" + file))
            {
                StorageFile localFile = await localFolder.GetFileAsync(file);
                Stream stream = await localFile.OpenStreamForReadAsync();
                using (var reader = new StreamReader(stream))
                {
                    props = new Dictionary<string, string>();
                    Debug.WriteLine("Loading " + localFolder.Path + "\\" + file);
                    loadTable(props, reader);
                }
            }
            else
            {
                throw new IOException("Did not find " + localFolder.Path + "\\" + file);
            }
        }

        /// <summary>
        /// Load property file from the OneWireAPI assembly
        /// </summary>
        /// <param name="resource_file"></param>
        public void loadResourceFile(Assembly asm, string file)
        {
            try
            {
                using (Stream stream = asm.GetManifestResourceStream(file))
                using (StreamReader reader = new StreamReader(stream))
                {
                    Debug.WriteLine("Loading resource: " + file);
                    props = new Dictionary<string, string>();
                    loadTable(props, reader);
                }
            }
            catch(Exception)
            {
                throw new IOException("Can't find resource: " + file);
            }
        }

        /// <summary>
        /// Look up key in hashtable for value
        /// </summary>
        /// <param name="propName"></param>
        /// <returns>null if no entry, otherwise string value</returns>
        public string getProperty(string propName)
        {
            string ret_str = null;

            if (props != null)
            {
                props.TryGetValue(propName, out ret_str);
            }
            return ret_str;
        }

        public string getProperty(string propName, string defValue)
        {
            string ret = getProperty(propName);
            return (string.ReferenceEquals(ret, null)) ? defValue : ret;
        }

        public bool getPropertyBoolean(string propName, bool defValue)
        {
            string strValue = getProperty(propName);
            if (!string.ReferenceEquals(strValue, null))
            {
                defValue = System.Convert.ToBoolean(strValue);
            }
            return defValue;
        }

        public byte[] getPropertyBytes(string propName, byte[] defValue)
        {
            string strValue = getProperty(propName);
            if (!string.ReferenceEquals(strValue, null))
            {
                //only supports up to 128 bytes of data
                byte[] tmp = new byte[128];

                //split the string on commas and spaces
                string[] strtok = strValue.Split(new Char[] { ',', ' ' });

                //how many bytes we got
                int i = 0;
                foreach (string multiByteStr in strtok)
                {
                    //this string could have more than one byte in it
                    int strLen = multiByteStr.Length;

                    for (int j = 0; j < strLen; j += 2)
                    {
                        //get just two nibbles at a time
                        string byteStr = multiByteStr.Substring(j, Math.Min(2, strLen));

                        long lng = 0;
                        try
                        {
                            //parse the two nibbles into a byte
                            lng = long.Parse(byteStr); //16
                        }
                        catch (FormatException nfe)
                        {
                            Debug.WriteLine(nfe.ToString());
                            Debug.Write(nfe.StackTrace);

                            //no mercy!
                            return defValue;
                        }

                        //store the byte and increment the counter
                        if (i < tmp.Length)
                        {
                            tmp[i++] = (byte)(lng & 0x0FF);
                        }
                    }
                }

                if (i > 0)
                {
                    byte[] retVal = new byte[i];
                    Array.Copy(tmp, 0, retVal, 0, i);
                    return retVal;
                }
            }
            return defValue;
        }
    }

}
