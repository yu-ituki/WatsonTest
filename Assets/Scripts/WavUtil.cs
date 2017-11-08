using UnityEngine;
using System.Collections;
using System.IO;

/// <summary>
/// Wevユーティリティ.
/// </summary>
public class WAVUtil {

	/// <summary>
	/// AudioClip -> 生Wav.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="clip"></param>
	/// <returns></returns>
	public static byte[] ToByte( AudioClip clip ) {
		var hz = clip.frequency;
		var channels = clip.channels;
		var samples = clip.samples;

		int allSize = channels * samples * 4;
		allSize += 44;

		System.IO.MemoryStream stream = new MemoryStream( new byte[ allSize ] );
		System.IO.BinaryWriter writer = new BinaryWriter( stream, System.Text.Encoding.UTF8 );

		writer.Write( System.Text.Encoding.UTF8.GetBytes( "RIFF" ), 0, 4 );
		writer.Write( System.BitConverter.GetBytes( stream.Length - 8 ), 0, 4 );
		writer.Write( System.Text.Encoding.UTF8.GetBytes( "WAVE" ), 0, 4 );
		writer.Write( System.Text.Encoding.UTF8.GetBytes( "fmt " ), 0, 4 );
		writer.Write( System.BitConverter.GetBytes( 16 ), 0, 4 );

		ushort one = 1;

		writer.Write( System.BitConverter.GetBytes( one ), 0, 2 );
		writer.Write( System.BitConverter.GetBytes( channels ), 0, 2 );
		writer.Write( System.BitConverter.GetBytes( hz ), 0, 4 );
		writer.Write( System.BitConverter.GetBytes( hz * channels * 2 ), 0, 4 );
		writer.Write( System.BitConverter.GetBytes( ( ushort )( channels * 2 ) ), 0, 2 );

		writer.Write( System.BitConverter.GetBytes( ( ushort )16 ), 0, 2 );

		writer.Write( System.Text.Encoding.UTF8.GetBytes( "data" ), 0, 4 );

		writer.Write( System.BitConverter.GetBytes( samples * channels * 2 ), 0, 4 );

		float[] orgData = new float[ channels * samples ];
		clip.GetData( orgData, 0 );

		const float rescaleFactor = 32767;
		short[] intData = new short[ orgData.Length ];
		byte[] byteData = new byte[ orgData.Length * 2 ];
		for ( int i = 0; i < orgData.Length; ++i ) {
			intData[ i ] = ( short )( orgData[ i ] * rescaleFactor );
		}
		System.Buffer.BlockCopy( intData, 0, byteData, 0, byteData.Length );
		writer.Write( byteData, 0, byteData.Length );
		stream.Flush();

		var ret = stream.ToArray();
		writer.Close();
		stream.Dispose();
		stream.Close();
		return ret;
	}

	/// <summary>
	/// 生wav -> AudioClip.
	/// </summary>
	/// <param name="name">AudioClipの名前.</param>
	/// <param name="wav">生wav値.</param>
	/// <returns></returns>
	public static AudioClip ToAudioClip( string name, byte[] wav ) {

		// Determine if mono or stereo
		var ChannelCount = wav[ 22 ];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

		// Get the frequency
		var Frequency = BytesToInt( wav, 24 );

		// Get past all the other sub chunks to get to the data subchunk:
		int pos = 12;   // First Subchunk ID from 12 to 16

		// Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
		while ( !( wav[ pos ] == 100 && wav[ pos + 1 ] == 97 && wav[ pos + 2 ] == 116 && wav[ pos + 3 ] == 97 ) ) {
			pos += 4;
			int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
			pos += 4 + chunkSize;
		}
		pos += 8;

		// Pos is now positioned to start of actual sound data.
		var SampleCount = ( wav.Length - pos ) / 2;     // 2 bytes per sample (16 bit sound mono)
		if ( ChannelCount == 2 ) SampleCount /= 2;        // 4 bytes per sample (16 bit stereo)

		// Allocate memory (right will be null if only mono sound)
		float[] RightChannel = null;
		var LeftChannel = new float[ SampleCount ];
		if ( ChannelCount == 2 ) RightChannel = new float[ SampleCount ];
		else RightChannel = null;

		// Write to double array/s:
		int i=0;
		while ( pos < wav.Length ) {
			LeftChannel[ i ] = BytesToFloat( wav[ pos ], wav[ pos + 1 ] );
			pos += 2;
			if ( ChannelCount == 2 ) {
				RightChannel[ i ] = BytesToFloat( wav[ pos ], wav[ pos + 1 ] );
				pos += 2;
			}
			i++;
		}
		float[] dat = null;
		if ( ChannelCount == 2 ) {
			int allCnt = LeftChannel.Length + RightChannel.Length;
			dat = new float[ allCnt ];
			int cnt = 0;
			for ( int j = 0; j < allCnt; j += 2 ) {
				dat[ j ] = LeftChannel[ cnt ];
				dat[ j + 1 ] = RightChannel[ cnt ];
				++cnt;
			}
		} else {
			dat = new float[ LeftChannel.Length ];
			LeftChannel.CopyTo( dat, 0 );
		}
		

		AudioClip ret = AudioClip.Create( name, SampleCount, ChannelCount, Frequency, false );
		ret.SetData( dat, 0 );
		return ret;
	}






	/// <summary>
	/// bytes -> Float( -1.0f ～ 1.0 ) に変換.
	/// </summary>
	/// <param name="firstByte"></param>
	/// <param name="secondByte"></param>
	/// <returns></returns>
	static float BytesToFloat( byte firstByte, byte secondByte ) {
		short s = (short)((secondByte << 8) | firstByte);
		return s / 32768.0F;
	}

	/// <summary>
	/// 4bytes -> Int に変換.
	/// </summary>
	/// <param name="bytes"></param>
	/// <param name="offset"></param>
	/// <returns></returns>
	static int BytesToInt( byte[] bytes, int offset = 0 ) {
		int value=0;
		for ( int i = 0; i < 4; i++ ) {
			value |= ( ( int )bytes[ offset + i ] ) << ( i * 8 );
		}
		return value;
	}
	


}
