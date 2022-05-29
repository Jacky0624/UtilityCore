using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Socket.Common
{
	public static class EofHelper
	{
		public const int EofLength = 5;
		private static readonly byte EofEscapeChar = Encoding.ASCII.GetBytes(new char[] { '<' })[0];
		private static readonly byte[] EofTail = Encoding.ASCII.GetBytes("EOF>");
		private static readonly byte[] NonEofTail = Encoding.ASCII.GetBytes("<EOF>");

		public static int AppendEof(byte[] data, int length)
		{
			int indexNow = length;
			int newlength = length + 1 + EofTail.Length;
			data[indexNow] = EofEscapeChar;
			++indexNow;
			for (int i = 0; i < EofTail.Length; ++i)
			{
				data[indexNow] = EofTail[i];
				++indexNow;
			}
			return newlength;
		}

		public static int FindFullPacket(byte[] data, int startIndex, int length, byte[] answerData, ref int answerLength)
		{
			answerLength = 0;
			int eofEndIndex = -1;
			int answerIndexNow = -1;
			bool eofFound = false;
			int maxIndex = startIndex + length - 1;
			for (int i = 0; i < length; ++i)
			{
				int currentIndex = startIndex + i;
				byte byteNow = data[currentIndex];
				if (byteNow == EofEscapeChar)
				{
					bool isNonEof = false;
					for (int whichEofTailMatch = 0; whichEofTailMatch < EofTail.Length; ++whichEofTailMatch)
					{
						int whichMatchIndex = currentIndex + 1 + whichEofTailMatch;
						if (whichMatchIndex > maxIndex || data[whichMatchIndex] != EofTail[whichEofTailMatch])
						{
							break;
						}
						else if (whichEofTailMatch >= EofTail.Length - 1)
						{
							eofFound = true;
							eofEndIndex = currentIndex + EofTail.Length;
							goto End;
						}
					}

					for (int whichNonEofTailMatch = 0; whichNonEofTailMatch < NonEofTail.Length; ++whichNonEofTailMatch)
					{
						int whichMatchIndex = currentIndex + 1 + whichNonEofTailMatch;
						if (whichMatchIndex > maxIndex || data[whichMatchIndex] != NonEofTail[whichNonEofTailMatch])
						{
							break;
						}
						else if (whichNonEofTailMatch >= NonEofTail.Length - 1)
						{
							for (int whichNonEofTail = 0; whichNonEofTail < NonEofTail.Length; ++whichNonEofTail)
							{
								++answerIndexNow;
								answerData[answerIndexNow] = data[currentIndex + 1 + whichNonEofTail];
							}
							i += NonEofTail.Length;
							isNonEof = true;
						}
					}

					if (!isNonEof)
					{
						++answerIndexNow;
						answerData[answerIndexNow] = data[currentIndex];
					}
				}
				else
				{
					++answerIndexNow;
					answerData[answerIndexNow] = data[currentIndex];
				}
			}
			End:
			if (eofFound)
			{
				answerLength = answerIndexNow + 1;
			}
			return eofEndIndex;
		}

		public static int FindFullPacketCustomEof(byte[] data, int startIndex, int length, ref int answerLength, byte[] eof)
		{
			answerLength = 0;
			int eofEndIndex = -1;
			for (int i = 0; i < length; ++i)
			{
				int currentIndex = startIndex + i;

				for (int whichEof = 0; whichEof < eof.Length; ++whichEof)
				{
					int currentEofIndex = currentIndex + whichEof;
					if (currentEofIndex >= startIndex + length)
					{
						goto END;
					}
					else if (data[currentIndex + whichEof] == eof[whichEof])
					{
						if (whichEof >= eof.Length - 1)
						{
							eofEndIndex = currentIndex;
							answerLength = currentIndex - startIndex + eof.Length;
							goto END;
						}
					}
					else
					{
						break;
					}
				}
			}
			END:
			return eofEndIndex;
		}
	}
}
