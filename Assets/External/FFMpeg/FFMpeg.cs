using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
public class FFMpeg {

	static readonly string PATH_FFMPEG_EXE = Application.dataPath + "/External/FFMpeg/ffmpeg.exe";

	/// <summary>
	/// ffmpegを使ってFlacを生成する.
	/// </summary>
	/// <param name="inPath"></param>
	/// <param name="outPath"></param>
	/// <param name="samplingRate"></param>
	public static string ToFlac( string inPath, string outPath, int samplingRate = 44100 ) {
		var args = string.Format( "-i \"{0}\" -vn -ar {2} -ac 2 -acodec flac -f flac \"{1}\"", inPath.Replace( "\\", "/" ), outPath.Replace( "\\", "/" ), samplingRate );
		Run( args );
		return outPath;
	}

	/// <summary>
	/// ffmpegを使って生Wavを生成する.
	/// </summary>
	/// <param name="inPath"></param>
	/// <param name="outPath"></param>
	/// <param name="samplingRate"></param>
	public static string ToWav( string inPath, string outPath, int samplingRate = 44100 ) {
		if ( !outPath.EndsWith( ".wav" ) ) {
			outPath += ".wav";
		}
		var args = string.Format( "-i \"{0}\" -vn -ar {2} -ac 2 \"{1}\"", inPath, outPath, samplingRate );
		Run( args );
		return outPath;
	}




	static void Run( string args ) {
		var process = Process.Start( new ProcessStartInfo( PATH_FFMPEG_EXE, args ) {
			CreateNoWindow = true,
			UseShellExecute = false,
		} );

		process.WaitForExit();
	}
}
