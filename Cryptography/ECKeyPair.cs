//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// ECKeyPair.cs - Copyright (c) 2018 Németh Péter
//-----------------------------------------------------------------------
// MicroCoin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MicroCoin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU General Public License for more details.
//-----------------------------------------------------------------------
// You should have received a copy of the GNU General Public License
// along with MicroCoin. If not, see <http://www.gnu.org/licenses/>.
//-----------------------------------------------------------------------

using MicroCoin.Util;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace MicroCoin.Cryptography
{
    public enum CurveType : ushort
    {
        Empty = 0,
        Secp256K1 = 714,
        Secp384R1 = 715,
        Secp521R1 = 716,
        Sect283K1 = 729
    }

    public class ECKeyPair : IEquatable<ECKeyPair>
    {
        private ECParameters? _eCParameters;
        public ECPoint PublicKey
        {
            get;
            set;
        } = new ECPoint()
        {
            X = new byte[0],
            Y = new byte[0]
        };
        public ECParameters ECParameters
        {
            get
            {
                if (_eCParameters != null) return _eCParameters.Value;                
                ECCurve curve = ECCurve.CreateFromFriendlyName(CurveType.ToString().ToLower());
                ECParameters parameters = new ECParameters
                {
                    Q = PublicKey
                };
                if (D != null)
                {
                    parameters.D = D;
                }

                parameters.Curve = curve;
                parameters.Validate();
                _eCParameters = parameters;
                return _eCParameters.Value;
            }
        }
        public CurveType CurveType { get; set; } = CurveType.Empty;
        public byte[] D { get; set; }        
        public BigInteger PrivateKey
        {
            get => D == null ? new BigInteger(0) : new BigInteger(D);
            set => D = value.ToByteArray();
        }

        public ByteString Name { get; set; }

        public static implicit operator ECParameters(ECKeyPair keyPair)
        {
            return keyPair.ECParameters;
        }

        public string ToEncodedString()
        {
            ByteString result = CurveType + ":" + PublicKey.X + ":" + PublicKey.Y;
            using (SHA256Managed managed = new SHA256Managed())
            {
                Hash hash = managed.ComputeHash(result);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (DeflateStream deflateStream = new DeflateStream(ms, CompressionLevel.Optimal, true))
                    {
                        ByteString bs = result + ":" + hash;
                        deflateStream.Write(bs, 0, bs.Length);
                    }
                    Hash h = ms.ToArray();
                    return h;
                }
            }
        }

        public static ECKeyPair FromEncodedString(Hash encodedString)
        {
            using (MemoryStream ms = new MemoryStream(encodedString, false))
            {
                using (DeflateStream deflateStream = new DeflateStream(ms, CompressionMode.Decompress, true))
                {
                    using (MemoryStream ms2 = new MemoryStream())
                    {
                        deflateStream.CopyTo(ms2);
                        ByteString hash = ms2.ToArray();
                        string encoded = hash;
                        var parts = encoded.Split(':');
                        ByteString check = parts[0] + ":" + parts[1] + ":" + parts[2];
                        using (SHA256Managed managed = new SHA256Managed())
                        {
                            Hash checkSum = managed.ComputeHash(check);
                            if (checkSum != parts[3])
                            {
                                throw new InvalidDataException("Invalid checksum");
                            }
                        }

                        Hash x = parts[1];
                        Hash y = parts[2];
                        ECKeyPair result = new ECKeyPair
                        {
                            CurveType = (CurveType) Enum.Parse(typeof(CurveType), parts[0]),
                            PublicKey = new ECPoint
                            {
                                X =  x,
                                Y =  y
                            }
                        };
                        return result;
                    }
                }
            }
        }

        public void SaveToStream(Stream s, bool writeLength = true, bool writePrivateKey = false,
            bool writeName = false)
        {
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                var len = PublicKey.X.Length + PublicKey.Y.Length + 6;

                if (writeName) Name.SaveToStream(bw);
                if (writeLength) bw.Write((ushort) len);
                bw.Write((ushort) CurveType);
                if (CurveType == CurveType.Empty)
                {
                    bw.Write((ushort) 0);
                    bw.Write((ushort) 0);
                    return;
                }

                ushort xLen = (ushort)PublicKey.X.Length;
                byte[] x = PublicKey.X;
                if (x[0] == 0) xLen--;
                bw.Write(xLen);
                bw.Write(x, x[0] == 0 ? 1 : 0, x.Length - (x[0] == 0 ? 1 : 0));
                ushort yLen;
                if (CurveType == CurveType.Sect283K1)
                {
                    byte[] b = PublicKey.Y;
                    yLen = (ushort)PublicKey.Y.Length;
                    if (b[0] == 0) yLen--;
                    bw.Write(yLen);
                    bw.Write(b, b[0] == 0 ? 1 : 0, b.Length - (b[0] == 0 ? 1 : 0));
                }
                else
                {
                    byte[] b = PublicKey.Y;
                    yLen = (ushort)PublicKey.Y.Length;
                    if (b[0] == 0) yLen--;
                    bw.Write(yLen);
                    bw.Write(b, b[0] == 0 ? 1 : 0, b.Length - (b[0] == 0 ? 1 : 0));
                }

                if (writePrivateKey)
                {
                    D.SaveToStream(bw);
                }
            }
        }

        public static ECKeyPair CreateNew(bool v, string name = "")
        {
#if NATIVE_ECDSA
            ECCurve curve = ECCurve.CreateFromFriendlyName("secP256k1".ToUpper());
            Console.WriteLine("Generating keys");
            var ecdsa = ECDsa.Create(curve);
            ECParameters parameters = ecdsa.ExportParameters(true);
            ECKeyPair pair = new ECKeyPair
            {
                CurveType = CurveType.Secp256K1,
                PublicKey = parameters.Q,
                D = parameters.D,
                Name = name
            };
            return pair;
#else
            SecureRandom secureRandom = new SecureRandom();
            X9ECParameters curve = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            ECKeyPairGenerator generator = new ECKeyPairGenerator();
            ECKeyGenerationParameters keygenParams = new ECKeyGenerationParameters(domain, secureRandom);
            generator.Init(keygenParams);
            AsymmetricCipherKeyPair keypair = generator.GenerateKeyPair();
            ECPrivateKeyParameters privParams = (ECPrivateKeyParameters)keypair.Private;
            ECPublicKeyParameters pubParams = (ECPublicKeyParameters)keypair.Public;
            ECKeyPair k = new ECKeyPair
            {
                CurveType = CurveType.Secp256K1,
                PrivateKey = new BigInteger(privParams.D.ToByteArray()),
                Name = name
            };
            k.PublicKey = new ECPoint
            {
                X = pubParams.Q.X.ToBigInteger().ToByteArray(),
                Y = pubParams.Q.Y.ToBigInteger().ToByteArray()
            };
            return k;

#endif
        }

        public void LoadFromStream(Stream stream, bool doubleLen = true, bool readPrivateKey = false,
            bool readName = false)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                if (readName) Name = ByteString.ReadFromStream(br);
                if (doubleLen)
                {
                    ushort len = br.ReadUInt16();
                    if (len == 0) return;
                }

                CurveType = (CurveType) br.ReadUInt16();
                ushort xLen = br.ReadUInt16();
                var X = br.ReadBytes(xLen);
                ushort yLen = br.ReadUInt16();
                var Y = br.ReadBytes(yLen);
                PublicKey = new ECPoint() { X = X, Y = Y };
                if (readPrivateKey)
                {
                    D = Hash.ReadFromStream(br);
                }
            }
        }

        public bool Equals(ECKeyPair other)
        {
            if (other == null) return false;
            if (!PublicKey.X.SequenceEqual(other.PublicKey.X)) return false;
            if (!PublicKey.Y.SequenceEqual(other.PublicKey.Y)) return false;
            return true;
        }

        public void DecriptKey(ByteString password)
        {
            byte[] b = new byte[32];
            var salt = D.Skip(8).Take(8).ToArray();
            SHA256Managed managed = new SHA256Managed();
            managed.Initialize();
            managed.TransformBlock(password, 0, password.Length, b, 0);
            managed.TransformFinalBlock(salt, 0, salt.Length);
            var digest = managed.Hash;
            managed.Dispose();
            managed = new SHA256Managed();
            managed.Initialize();
            managed.TransformBlock(digest, 0, digest.Length, b, 0);
            managed.TransformBlock(password, 0, password.Length, b, 0);
            salt = D.Skip(8).Take(8).ToArray();
            managed.TransformFinalBlock(salt, 0, salt.Length);
            var iv = managed.Hash;
            managed.Dispose();
            RijndaelManaged aesEncryption = new RijndaelManaged
            {
                KeySize = 256,
                BlockSize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            byte[] encryptedBytes = D.Skip(16).ToArray(); //Crazy Salt...
            aesEncryption.IV = iv.Take(16).ToArray();
            aesEncryption.Key = digest;
            ICryptoTransform decrypto = aesEncryption.CreateDecryptor();
            Hash hash = decrypto.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            ByteString bs = hash;
            Hash h2 = bs.ToString(); // dirty hack
            D = h2;
            aesEncryption.Dispose();
        }

        public override string ToString()
        {
            return $"{Name} - {CurveType}";
        }
    }
}
