﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Lokad.Quality;

namespace Lokad.Cloud.Azure
{
	/// <summary>
	/// Encrypts and decrypts data using DPAPI functions.
	/// </summary>
	[NoCodeCoverage]
	public class DBAPI
	{
		// Wrapper for DPAPI CryptProtectData function.
		[DllImport("crypt32.dll",
					SetLastError = true,
					CharSet = CharSet.Auto)]
		private static extern
			bool CryptProtectData(ref DATA_BLOB pPlainText,
										string szDescription,
									ref DATA_BLOB pEntropy,
										IntPtr pReserved,
									ref CRYPTPROTECT_PROMPTSTRUCT pPrompt,
										int dwFlags,
									ref DATA_BLOB pCipherText);

		// Wrapper for DPAPI CryptUnprotectData function.
		[DllImport("crypt32.dll",
					SetLastError = true,
					CharSet = CharSet.Auto)]
		private static extern
			bool CryptUnprotectData(ref DATA_BLOB pCipherText,
									ref string pszDescription,
									ref DATA_BLOB pEntropy,
										IntPtr pReserved,
									ref CRYPTPROTECT_PROMPTSTRUCT pPrompt,
										int dwFlags,
									ref DATA_BLOB pPlainText);

		// BLOB structure used to pass data to DPAPI functions.
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct DATA_BLOB
		{
			public int cbData;
			public IntPtr pbData;
		}

		// Prompt structure to be used for required parameters.
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct CRYPTPROTECT_PROMPTSTRUCT
		{
			public int cbSize;
			public int dwPromptFlags;
			public IntPtr hwndApp;
			public string szPrompt;
		}

		// Wrapper for the NULL handle or pointer.
		static private readonly IntPtr NullPtr = ((IntPtr)0);

		// DPAPI key initialization flags.
		private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;
		private const int CRYPTPROTECT_LOCAL_MACHINE = 0x4;

		/// <summary>
		/// Initializes empty prompt structure.
		/// </summary>
		/// <param name="ps">
		/// Prompt parameter (which we do not actually need).
		/// </param>
		private static void InitPrompt(ref CRYPTPROTECT_PROMPTSTRUCT ps)
		{
			ps.cbSize = Marshal.SizeOf(
									  typeof(CRYPTPROTECT_PROMPTSTRUCT));
			ps.dwPromptFlags = 0;
			ps.hwndApp = NullPtr;
			ps.szPrompt = null;
		}

		/// <summary>
		/// Initializes a BLOB structure from a byte array.
		/// </summary>
		/// <param name="data">
		/// Original data in a byte array format.
		/// </param>
		/// <param name="blob">
		/// Returned blob structure.
		/// </param>
		private static void InitBlob(byte[] data, ref DATA_BLOB blob)
		{
			// Use empty array for null parameter.
			if (data == null)
				data = new byte[0];

			// Allocate memory for the BLOB data.
			blob.pbData = Marshal.AllocHGlobal(data.Length);

			// Make sure that memory allocation was successful.
			if (blob.pbData == IntPtr.Zero)
			{
				throw new ApplicationException("Unable to allocate data buffer for BLOB structure.");
			}

			// Specify number of bytes in the BLOB.
			blob.cbData = data.Length;

			// Copy data from original source to the BLOB structure.
			Marshal.Copy(data, 0, blob.pbData, data.Length);
		}

		/// <summary>Flag indicating the type of key. DPAPI terminology refers to
		/// key types as user store or machine store.</summary>
		public enum KeyType
		{
			/// <summary>User store.</summary>
			UserKey = 1,

			/// <summary>Machine store.</summary>
			MachineKey
		};

		// It is reasonable to set default key type to user key.
		private const KeyType DefaultKeyType = KeyType.UserKey;

		/// <summary>
		/// Calls DPAPI CryptProtectData function to encrypt a plaintext
		/// string value with a user-specific key. This function does not
		/// specify data description and additional entropy.
		/// </summary>
		/// <param name="plainText">
		/// Plaintext data to be encrypted.
		/// </param>
		/// <returns>
		/// Encrypted value in a base64-encoded format.
		/// </returns>
		public static string Encrypt(string plainText)
		{
			return Encrypt(DefaultKeyType, plainText, String.Empty,
							String.Empty);
		}

		/// <summary>
		/// Calls DPAPI CryptProtectData function to encrypt a plaintext
		/// string value. This function does not specify data description
		/// and additional entropy.
		/// </summary>
		/// <param name="keyType">
		/// Defines type of encryption key to use. When user key is
		/// specified, any application running under the same user account
		/// as the one making this call, will be able to decrypt data.
		/// Machine key will allow any application running on the same
		/// computer where data were encrypted to perform decryption.
		/// Note: If optional entropy is specifed, it will be required
		/// for decryption.
		/// </param>
		/// <param name="plainText">
		/// Plaintext data to be encrypted.
		/// </param>
		/// <returns>
		/// Encrypted value in a base64-encoded format.
		/// </returns>
		public static string Encrypt(KeyType keyType, string plainText)
		{
			return Encrypt(keyType, plainText, String.Empty,
							String.Empty);
		}

		/// <summary>
		/// Calls DPAPI CryptProtectData function to encrypt a plaintext
		/// string value. This function does not specify data description.
		/// </summary>
		/// <param name="keyType">
		/// Defines type of encryption key to use. When user key is
		/// specified, any application running under the same user account
		/// as the one making this call, will be able to decrypt data.
		/// Machine key will allow any application running on the same
		/// computer where data were encrypted to perform decryption.
		/// Note: If optional entropy is specifed, it will be required
		/// for decryption.
		/// </param>
		/// <param name="plainText">
		/// Plaintext data to be encrypted.
		/// </param>
		/// <param name="entropy">
		/// Optional entropy which - if specified - will be required to
		/// perform decryption.
		/// </param>
		/// <returns>
		/// Encrypted value in a base64-encoded format.
		/// </returns>
		public static string Encrypt(KeyType keyType,
									 string plainText,
									 string entropy)
		{
			return Encrypt(keyType, plainText, entropy, String.Empty);
		}

		/// <summary>
		/// Calls DPAPI CryptProtectData function to encrypt a plaintext
		/// string value.
		/// </summary>
		/// <param name="keyType">
		/// Defines type of encryption key to use. When user key is
		/// specified, any application running under the same user account
		/// as the one making this call, will be able to decrypt data.
		/// Machine key will allow any application running on the same
		/// computer where data were encrypted to perform decryption.
		/// Note: If optional entropy is specifed, it will be required
		/// for decryption.
		/// </param>
		/// <param name="plainText">
		/// Plaintext data to be encrypted.
		/// </param>
		/// <param name="entropy">
		/// Optional entropy which - if specified - will be required to
		/// perform decryption.
		/// </param>
		/// <param name="description">
		/// Optional description of data to be encrypted. If this value is
		/// specified, it will be stored along with encrypted data and
		/// returned as a separate value during decryption.
		/// </param>
		/// <returns>
		/// Encrypted value in a base64-encoded format.
		/// </returns>
		public static string Encrypt(KeyType keyType,
									 string plainText,
									 string entropy,
									 string description)
		{
			// Make sure that parameters are valid.
			if (plainText == null) plainText = String.Empty;
			if (entropy == null) entropy = String.Empty;

			// Call encryption routine and convert returned bytes into
			// a base64-encoded value.
			return Convert.ToBase64String(
					Encrypt(keyType,
							Encoding.UTF8.GetBytes(plainText),
							Encoding.UTF8.GetBytes(entropy),
							description));
		}

		/// <summary>
		/// Calls DPAPI CryptProtectData function to encrypt an array of
		/// plaintext bytes.
		/// </summary>
		/// <param name="keyType">
		/// Defines type of encryption key to use. When user key is
		/// specified, any application running under the same user account
		/// as the one making this call, will be able to decrypt data.
		/// Machine key will allow any application running on the same
		/// computer where data were encrypted to perform decryption.
		/// Note: If optional entropy is specifed, it will be required
		/// for decryption.
		/// </param>
		/// <param name="plainTextBytes">
		/// Plaintext data to be encrypted.
		/// </param>
		/// <param name="entropyBytes">
		/// Optional entropy which - if specified - will be required to
		/// perform decryption.
		/// </param>
		/// <param name="description">
		/// Optional description of data to be encrypted. If this value is
		/// specified, it will be stored along with encrypted data and
		/// returned as a separate value during decryption.
		/// </param>
		/// <returns>
		/// Encrypted value.
		/// </returns>
		public static byte[] Encrypt(KeyType keyType,
									 byte[] plainTextBytes,
									 byte[] entropyBytes,
									 string description)
		{
			// Make sure that parameters are valid.
			if (plainTextBytes == null) plainTextBytes = new byte[0];
			if (entropyBytes == null) entropyBytes = new byte[0];
			if (description == null) description = String.Empty;

			// Create BLOBs to hold data.
			var plainTextBlob = new DATA_BLOB();
			var cipherTextBlob = new DATA_BLOB();
			var entropyBlob = new DATA_BLOB();

			// We only need prompt structure because it is a required
			// parameter.
			var prompt = new CRYPTPROTECT_PROMPTSTRUCT();
			InitPrompt(ref prompt);

			try
			{
				// Convert plaintext bytes into a BLOB structure.
				try
				{
					InitBlob(plainTextBytes, ref plainTextBlob);
				}
				catch (Exception ex)
				{
					throw new ArgumentException("Cannot initialize plaintext BLOB.", ex);
				}

				// Convert entropy bytes into a BLOB structure.
				try
				{
					InitBlob(entropyBytes, ref entropyBlob);
				}
				catch (Exception ex)
				{
					throw new ArgumentException("Cannot initialize entropy BLOB.", ex);
				}

				// Disable any types of UI.
				int flags = CRYPTPROTECT_UI_FORBIDDEN;

				// When using machine-specific key, set up machine flag.
				if (keyType == KeyType.MachineKey)
					flags |= CRYPTPROTECT_LOCAL_MACHINE;

				// Call DPAPI to encrypt data.
				bool success = CryptProtectData(ref plainTextBlob,
													description,
												ref entropyBlob,
													IntPtr.Zero,
												ref prompt,
													flags,
												ref cipherTextBlob);
				// Check the result.
				if (!success)
				{
					// If operation failed, retrieve last Win32 error.
					int errCode = Marshal.GetLastWin32Error();

					// Win32Exception will contain error message corresponding
					// to the Windows error code.
					throw new InvalidOperationException("CryptProtectData failed.", new Win32Exception(errCode));
				}

				// Allocate memory to hold ciphertext.
				var cipherTextBytes = new byte[cipherTextBlob.cbData];

				// Copy ciphertext from the BLOB to a byte array.
				Marshal.Copy(cipherTextBlob.pbData,
								cipherTextBytes,
								0,
								cipherTextBlob.cbData);

				// Return the result.
				return cipherTextBytes;
			}
			// Free all memory allocated for BLOBs.
			finally
			{
				if (plainTextBlob.pbData != IntPtr.Zero)
					Marshal.FreeHGlobal(plainTextBlob.pbData);

				if (cipherTextBlob.pbData != IntPtr.Zero)
					Marshal.FreeHGlobal(cipherTextBlob.pbData);

				if (entropyBlob.pbData != IntPtr.Zero)
					Marshal.FreeHGlobal(entropyBlob.pbData);
			}
		}

		/// <summary>
		/// Calls DPAPI CryptUnprotectData to decrypt ciphertext bytes.
		/// This function does not use additional entropy and does not
		/// return data description.
		/// </summary>
		/// <param name="cipherText">
		/// Encrypted data formatted as a base64-encoded string.
		/// </param>
		/// <returns>
		/// Decrypted data returned as a UTF-8 string.
		/// </returns>
		/// <remarks>
		/// When decrypting data, it is not necessary to specify which
		/// type of encryption key to use: user-specific or
		/// machine-specific; DPAPI will figure it out by looking at
		/// the signature of encrypted data.
		/// </remarks>
		public static string Decrypt(string cipherText)
		{
			string description;

			return Decrypt(cipherText, String.Empty, out description);
		}

		/// <summary>
		/// Calls DPAPI CryptUnprotectData to decrypt ciphertext bytes.
		/// This function does not use additional entropy.
		/// </summary>
		/// <param name="cipherText">
		/// Encrypted data formatted as a base64-encoded string.
		/// </param>
		/// <param name="description">
		/// Returned description of data specified during encryption.
		/// </param>
		/// <returns>
		/// Decrypted data returned as a UTF-8 string.
		/// </returns>
		/// <remarks>
		/// When decrypting data, it is not necessary to specify which
		/// type of encryption key to use: user-specific or
		/// machine-specific; DPAPI will figure it out by looking at
		/// the signature of encrypted data.
		/// </remarks>
		public static string Decrypt(string cipherText,
									 out string description)
		{
			return Decrypt(cipherText, String.Empty, out description);
		}

		/// <summary>
		/// Calls DPAPI CryptUnprotectData to decrypt ciphertext bytes.
		/// </summary>
		/// <param name="cipherText">
		/// Encrypted data formatted as a base64-encoded string.
		/// </param>
		/// <param name="entropy">
		/// Optional entropy, which is required if it was specified during
		/// encryption.
		/// </param>
		/// <param name="description">
		/// Returned description of data specified during encryption.
		/// </param>
		/// <returns>
		/// Decrypted data returned as a UTF-8 string.
		/// </returns>
		/// <remarks>
		/// When decrypting data, it is not necessary to specify which
		/// type of encryption key to use: user-specific or
		/// machine-specific; DPAPI will figure it out by looking at
		/// the signature of encrypted data.
		/// </remarks>
		public static string Decrypt(string cipherText,
										 string entropy,
									 out string description)
		{
			// Make sure that parameters are valid.
			if (entropy == null) entropy = String.Empty;

			return Encoding.UTF8.GetString(
						Decrypt(Convert.FromBase64String(cipherText),
									Encoding.UTF8.GetBytes(entropy),
								out description));
		}

		/// <summary>
		/// Calls DPAPI CryptUnprotectData to decrypt ciphertext bytes.
		/// </summary>
		/// <param name="cipherTextBytes">
		/// Encrypted data.
		/// </param>
		/// <param name="entropyBytes">
		/// Optional entropy, which is required if it was specified during
		/// encryption.
		/// </param>
		/// <param name="description">
		/// Returned description of data specified during encryption.
		/// </param>
		/// <returns>
		/// Decrypted data bytes.
		/// </returns>
		/// <remarks>
		/// When decrypting data, it is not necessary to specify which
		/// type of encryption key to use: user-specific or
		/// machine-specific; DPAPI will figure it out by looking at
		/// the signature of encrypted data.
		/// </remarks>
		public static byte[] Decrypt(byte[] cipherTextBytes,
										 byte[] entropyBytes,
									 out string description)
		{
			// Create BLOBs to hold data.
			var plainTextBlob = new DATA_BLOB();
			var cipherTextBlob = new DATA_BLOB();
			var entropyBlob = new DATA_BLOB();

			// We only need prompt structure because it is a required
			// parameter.
			var prompt = new CRYPTPROTECT_PROMPTSTRUCT();
			InitPrompt(ref prompt);

			// Initialize description string.
			description = String.Empty;

			try
			{
				// Convert ciphertext bytes into a BLOB structure.
				try
				{
					InitBlob(cipherTextBytes, ref cipherTextBlob);
				}
				catch (Exception ex)
				{
					throw new ArgumentException("Cannot initialize ciphertext BLOB.", ex);
				}

				// Convert entropy bytes into a BLOB structure.
				try
				{
					InitBlob(entropyBytes, ref entropyBlob);
				}
				catch (Exception ex)
				{
					throw new ArgumentException("Cannot initialize entropy BLOB.", ex);
				}

				// Disable any types of UI. CryptUnprotectData does not
				// mention CRYPTPROTECT_LOCAL_MACHINE flag in the list of
				// supported flags so we will not set it up.
				var flags = CRYPTPROTECT_UI_FORBIDDEN;

				// Call DPAPI to decrypt data.
				var success = CryptUnprotectData(ref cipherTextBlob,
												  ref description,
												  ref entropyBlob,
													  IntPtr.Zero,
												  ref prompt,
													  flags,
												  ref plainTextBlob);

				// Check the result.
				if (!success)
				{
					// If operation failed, retrieve last Win32 error.
					int errCode = Marshal.GetLastWin32Error();

					// Win32Exception will contain error message corresponding
					// to the Windows error code.
					throw new InvalidOperationException("CryptUnprotectData failed.", new Win32Exception(errCode));
				}

				// Allocate memory to hold plaintext.
				var plainTextBytes = new byte[plainTextBlob.cbData];

				// Copy ciphertext from the BLOB to a byte array.
				Marshal.Copy(plainTextBlob.pbData,
							 plainTextBytes,
							 0,
							 plainTextBlob.cbData);

				// Return the result.
				return plainTextBytes;
			}
			// Free all memory allocated for BLOBs.
			finally
			{
				if (plainTextBlob.pbData != IntPtr.Zero)
					Marshal.FreeHGlobal(plainTextBlob.pbData);

				if (cipherTextBlob.pbData != IntPtr.Zero)
					Marshal.FreeHGlobal(cipherTextBlob.pbData);

				if (entropyBlob.pbData != IntPtr.Zero)
					Marshal.FreeHGlobal(entropyBlob.pbData);
			}
		}
	}
}
