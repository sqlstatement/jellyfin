using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Model.Cryptography
{
    public class PasswordHash
    {
        // Defined from this hash storage spec
        // https://github.com/P-H-C/phc-string-format/blob/master/phc-sf-spec.md
        // $<id>[$<param>=<value>(,<param>=<value>)*][$<salt>[$<hash>]]
        // with one slight amendment to ease the transition, we're writing out the bytes in hex
        // rather than making them a BASE64 string with stripped padding

        private string _id;

        private Dictionary<string, string> _parameters = new Dictionary<string, string>();

        private string _salt;

        private byte[] _saltBytes;

        private string _hash;

        private byte[] _hashBytes;

        public string Id { get => _id; set => _id = value; }

        public Dictionary<string, string> Parameters { get => _parameters; set => _parameters = value; }

        public string Salt { get => _salt; set => _salt = value; }

        public byte[] SaltBytes { get => _saltBytes; set => _saltBytes = value; }

        public string Hash { get => _hash; set => _hash = value; }

        public byte[] HashBytes { get => _hashBytes; set => _hashBytes = value; }

        public PasswordHash(string storageString)
        {
            string[] splitted = storageString.Split('$');
            _id = splitted[1];
            if (splitted[2].Contains("="))
            {
                foreach (string paramset in (splitted[2].Split(',')))
                {
                    if (!string.IsNullOrEmpty(paramset))
                    {
                        string[] fields = paramset.Split('=');
                        if (fields.Length == 2)
                        {
                            _parameters.Add(fields[0], fields[1]);
                        }
                        else
                        {
                            throw new Exception($"Malformed parameter in password hash string {paramset}");
                        }
                    }
                }
                if (splitted.Length == 5)
                {
                    _salt = splitted[3];
                    _saltBytes = ConvertFromByteString(_salt);
                    _hash = splitted[4];
                    _hashBytes = ConvertFromByteString(_hash);
                }
                else
                {
                    _salt = string.Empty;
                    _hash = splitted[3];
                    _hashBytes = ConvertFromByteString(_hash);
                }
            }
            else
            {
                if (splitted.Length == 4)
                {
                    _salt = splitted[2];
                    _saltBytes = ConvertFromByteString(_salt);
                    _hash = splitted[3];
                    _hashBytes = ConvertFromByteString(_hash);
                }
                else
                {
                    _salt = string.Empty;
                    _hash = splitted[2];
                    _hashBytes = ConvertFromByteString(_hash);
                }

            }

        }

        public PasswordHash(ICryptoProvider cryptoProvider)
        {
            _id = cryptoProvider.DefaultHashMethod;
            _saltBytes = cryptoProvider.GenerateSalt();
            _salt = ConvertToByteString(SaltBytes);
        }

        public static byte[] ConvertFromByteString(string byteString)
        {
            List<byte> Bytes = new List<byte>();
            for (int i = 0; i < byteString.Length; i += 2)
            {
                Bytes.Add(Convert.ToByte(byteString.Substring(i, 2),16));
            }

            return Bytes.ToArray();
        }

        public static string ConvertToByteString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private string SerializeParameters()
        {
            string ReturnString = string.Empty;
            foreach (var KVP in _parameters)
            {
                ReturnString += $",{KVP.Key}={KVP.Value}";
            }

            if ((!string.IsNullOrEmpty(ReturnString)) && ReturnString[0] == ',')
            {
                ReturnString = ReturnString.Remove(0, 1);
            }

            return ReturnString;
        }

        public override string ToString()
        {
            string outString = "$" +_id;
            string paramstring = SerializeParameters();
            if (!string.IsNullOrEmpty(paramstring))
            {
                outString += $"${paramstring}";
            }

            if (!string.IsNullOrEmpty(_salt))
            {
                outString += $"${_salt}";
            }

            outString += $"${_hash}";
            return outString;
        }
    }

}
