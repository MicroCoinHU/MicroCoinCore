//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// Utils.cs - Copyright (c) 2018 Németh Péter
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
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace MicroCoin.Cryptography
{
    public static class Utils
    {
        public static ECSignature GenerateSignature(Hash data, ECKeyPair keyPair)
        {
#if NATIVE_ECDSA
            using (var eC = ECDsa.Create(keyPair))
            {
                Hash sign = eC.SignHash(data);
                return new ECSignature(sign);
                 
            }
#else
            try
            {
                ISigner signer = SignerUtilities.GetSigner("NONEwithECDSA");
                X9ECParameters curve = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName(keyPair.CurveType.ToString().ToLower());
                ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
                Org.BouncyCastle.Math.BigInteger bn = new Org.BouncyCastle.Math.BigInteger(keyPair.D);
                ECPrivateKeyParameters parameters = new ECPrivateKeyParameters(bn, domain);
                signer.Init(true, parameters);
                signer.BlockUpdate(data, 0, data.Length);
                byte[] sigBytes = signer.GenerateSignature();
                Asn1InputStream decoder = new Asn1InputStream(sigBytes);
                DerSequence seq = (DerSequence)decoder.ReadObject();
                DerInteger r = (DerInteger)seq[0];
                DerInteger s = (DerInteger)seq[1];
                decoder.Dispose();
                var rArr = r.Value.ToByteArray();
                var sArr = s.Value.ToByteArray();
                if (rArr[0] == 0) rArr = rArr.Skip(1).ToArray();
                if (sArr[0] == 0) sArr = sArr.Skip(1).ToArray();
                return new ECSignature
                {
                    R = r.Value.ToByteArray(),
                    S = s.Value.ToByteArray(),
                    SigCompat = sigBytes
                };
            }
            catch (Exception e)
            {
                return new ECSignature();
            }
#endif
        }

        public static bool ValidateSignature(Hash data, ECSignature signature, ECKeyPair keyPair)
        {
#if NATIVE_ECDSA
            using (var ecdsa = ECDsa.Create(keyPair))
            {
                return ecdsa.VerifyHash(data, signature.Signature);                
            }
#else

            var derSignature = new DerSequence(
                new DerInteger(new Org.BouncyCastle.Math.BigInteger(1, signature.Signature.Take(32).ToArray())),
                new DerInteger(new Org.BouncyCastle.Math.BigInteger(1, signature.Signature.Skip(32).ToArray())))
                .GetDerEncoded();

            ISigner signer = SignerUtilities.GetSigner("NONEwithECDSA");
            X9ECParameters curve = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName(keyPair.CurveType.ToString().ToLower());
            ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            //Org.BouncyCastle.Math.BigInteger bn = new Org.BouncyCastle.Math.BigInteger(keyPair.D);
            //ECPrivateKeyParameters parameters = new ECPrivateKeyParameters(bn, domain);
            FpCurve c = (FpCurve)curve.Curve;
            var publicKey = new FpPoint(c, new FpFieldElement(c.Q, new Org.BouncyCastle.Math.BigInteger(+1, keyPair.PublicKey.X)),
                new FpFieldElement(c.Q, new Org.BouncyCastle.Math.BigInteger(+1, keyPair.PublicKey.Y)));
            ECPublicKeyParameters publicKeyParameters = new ECPublicKeyParameters(publicKey, domain);
            signer.Init(false, publicKeyParameters);
            signer.BlockUpdate(data, 0, data.Length);
            bool ok = signer.VerifySignature(derSignature);
            return ok;

#endif
        }

        public static ByteString DecryptString(Hash em, ECKeyPair keyPair)
        {
#if NETCOREAPP
            throw new NotImplementedException();
#else
            ECParameters parameters = new ECParameters();
            byte[] ems = em;
            parameters.Q.X = ems.Skip(1).Take(32).ToArray();
            parameters.Q.Y = ems.Skip(33).Take(32).ToArray();
            parameters.D = keyPair.D;
            parameters.Curve = keyPair.ECParameters.Curve;
            parameters.Validate();
            using (var ecDiffieHellmanCng = new ECDiffieHellmanCng())
            {
                ecDiffieHellmanCng.ImportParameters(parameters);
                var ek = ecDiffieHellmanCng.DeriveKeyFromHash(ecDiffieHellmanCng.PublicKey, HashAlgorithmName.SHA256, null, new byte[] { 0, 0, 0, 1 });
                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    aes.Padding = PaddingMode.PKCS7;
                    var d = aes.CreateDecryptor(ek, new byte[16]);
                    var bs = d.TransformFinalBlock(ems.Skip(65).Take(ems.Length - 65 - 32).ToArray(), 0,
                        ems.Length - 65 - 32);
                    return bs;
                }
            }
#endif
        }

        public static Hash Sha256(Hash data)
        {
            using (SHA256Managed sha = new SHA256Managed())
            {
                return sha.ComputeHash(data);
            }
        }

        public static Hash DoubleSha256(Hash data)
        {
            using (SHA256Managed sha = new SHA256Managed())
            {
                Hash h = sha.ComputeHash(data);
                return sha.ComputeHash(h);
            }
        }

        public static Hash RipeMD160(Hash data)
        {
#if NETCOREAPP
            var digest = new Org.BouncyCastle.Crypto.Digests.RipeMD160Digest();
            digest.BlockUpdate(data, 0, data.Length);
            Hash h = new byte[digest.GetDigestSize()];
            digest.DoFinal(h, 0);
            return h;            
#else
            using (RIPEMD160Managed rp = new RIPEMD160Managed())
            {
                return rp.ComputeHash(data);
            }
#endif
        }

        public static Hash EncryptString(ByteString data, ECKeyPair keyPair)
        {
#if NETCOREAPP
            throw new NotImplementedException();
#else
            using (var ecDiffieHellman = ECDiffieHellman.Create(keyPair))
            {
                using (var ephem = ECDiffieHellman.Create(keyPair.ECParameters.Curve))
                {
                    ECParameters ephemPublicParams = ephem.ExportParameters(false);
                    int pointLen = ephemPublicParams.Q.X.Length;
                    byte[] rBar = new byte[pointLen * 2 + 1];
                    rBar[0] = (byte)(keyPair.PublicKey.X.Length + keyPair.PublicKey.Y.Length);
                    Buffer.BlockCopy(ephemPublicParams.Q.X, 0, rBar, 1, pointLen);
                    Buffer.BlockCopy(ephemPublicParams.Q.Y, 0, rBar, 1 + pointLen, pointLen);
                    var ek = ephem.DeriveKeyFromHash(ecDiffieHellman.PublicKey, HashAlgorithmName.SHA256, null,
                        new byte[] { 0, 0, 0, 1 });
                    var mk = ephem.DeriveKeyFromHash(ecDiffieHellman.PublicKey, HashAlgorithmName.SHA256, null,
                        new byte[] { 0, 0, 0, 2 });

                    using (RijndaelManaged aes = new RijndaelManaged())
                    {
                        aes.Padding = PaddingMode.PKCS7;
                        using (ICryptoTransform encryptor = aes.CreateEncryptor(ek, new byte[16]))
                        {
                            if (!encryptor.CanTransformMultipleBlocks)
                            {
                                throw new InvalidOperationException();
                            }

                            Hash em = encryptor.TransformFinalBlock(data, 0, data.Length);
                            byte[] da;
                            using (HMAC hmac = new HMACSHA256(mk))
                            {
                                da = hmac.ComputeHash(em);
                            }

                            return rBar.Concat((byte[])em).Concat(da).ToArray();
                        }
                    }
                }
            }
#endif
        }
    }
}
