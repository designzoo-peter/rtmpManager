﻿using UnityEngine;
using System.Collections;

public static class StringByteExtensions {

	//String To Byte
	public static byte[] ConvertToBytes(this string str)
	{
		byte[] bytes = new byte[str.Length * sizeof(char)];
		System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
		return bytes;
	}
	//Byte To String 
	public static string ConvertToString(this byte[] bytes)
	{
		char[] chars = new char[bytes.Length / sizeof(char)];
		System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
		return new string(chars);
	}
}
