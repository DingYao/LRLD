using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace LRLD
{
	public static class ExtensionMethods
	{
		[CompilerGenerated]
		[Serializable]
		private sealed class <>c
		{
			public static readonly ExtensionMethods.<>c <>9 = new ExtensionMethods.<>c();

			internal void cctor>b__11_0()
			{
			}
		}

		private static readonly Action EmptyDelegate = new Action(ExtensionMethods.<>c.<>9.<.cctor>b__11_0);

		private static int _iterations = 2;

		private static int _keySize = 256;

		private static string _hash = "SHA1";

		private static string _salt = "";

		private static string _vector = "";

		public static void Refresh(this UIElement uiElement)
		{
			uiElement.Dispatcher.Invoke(DispatcherPriority.Render, ExtensionMethods.EmptyDelegate);
		}

		public static string Decrypt(string value, string password)
		{
			return ExtensionMethods.Decrypt<AesManaged>(value, password);
		}

		public static string Decrypt<T>(string value, string password) where T : SymmetricAlgorithm, new()
		{
			byte[] vectorBytes = Encoding.ASCII.GetBytes(ExtensionMethods._vector);
			byte[] saltBytes = Encoding.ASCII.GetBytes(ExtensionMethods._salt);
			byte[] valueBytes = Convert.FromBase64String(value);
			int decryptedByteCount = 0;
			byte[] decrypted;
			using (T cipher = Activator.CreateInstance<T>())
			{
				byte[] keyBytes = new PasswordDeriveBytes(password, saltBytes, ExtensionMethods._hash, ExtensionMethods._iterations).GetBytes(ExtensionMethods._keySize / 8);
				cipher.Mode = CipherMode.CBC;
				try
				{
					using (ICryptoTransform decryptor = cipher.CreateDecryptor(keyBytes, vectorBytes))
					{
						using (MemoryStream from = new MemoryStream(valueBytes))
						{
							using (CryptoStream reader = new CryptoStream(from, decryptor, CryptoStreamMode.Read))
							{
								decrypted = new byte[valueBytes.Length];
								decryptedByteCount = reader.Read(decrypted, 0, decrypted.Length);
							}
						}
					}
				}
				catch (Exception)
				{
					return string.Empty;
				}
				cipher.Clear();
			}
			return Encoding.UTF8.GetString(decrypted, 0, decryptedByteCount);
		}

		public static string Encrypt(string value, string password)
		{
			return ExtensionMethods.Encrypt<AesManaged>(value, password);
		}

		public static string Encrypt<T>(string value, string password) where T : SymmetricAlgorithm, new()
		{
			byte[] vectorBytes = Encoding.ASCII.GetBytes(ExtensionMethods._vector);
			byte[] saltBytes = Encoding.ASCII.GetBytes(ExtensionMethods._salt);
			byte[] valueBytes = Encoding.ASCII.GetBytes(value);
			byte[] encrypted;
			using (T cipher = Activator.CreateInstance<T>())
			{
				byte[] keyBytes = new PasswordDeriveBytes(password, saltBytes, ExtensionMethods._hash, ExtensionMethods._iterations).GetBytes(ExtensionMethods._keySize / 8);
				cipher.Mode = CipherMode.CBC;
				using (ICryptoTransform encryptor = cipher.CreateEncryptor(keyBytes, vectorBytes))
				{
					using (MemoryStream to = new MemoryStream())
					{
						using (CryptoStream writer = new CryptoStream(to, encryptor, CryptoStreamMode.Write))
						{
							writer.Write(valueBytes, 0, valueBytes.Length);
							writer.FlushFinalBlock();
							encrypted = to.ToArray();
						}
					}
				}
				cipher.Clear();
			}
			return Convert.ToBase64String(encrypted);
		}
	}
}
