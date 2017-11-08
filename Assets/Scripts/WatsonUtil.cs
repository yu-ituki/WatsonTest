using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Watsonに汚物を送りつけるユーティリティ.
/// </summary>
public class WatsonUtil {

	const string TOKEN_REST_URL = "https://gateway.watsonplatform.net/authorization/api/v1/token";
	const string TOKEN_STREAM_URL = "https://stream.watsonplatform.net/authorization/api/v1/token";

	/// <summary>
	/// 送りつける.
	/// </summary>
	/// <param name="url">URL</param>
	/// <param name="username">APIごとの認証ユーザ名.</param>
	/// <param name="pass">APIごとの認証ユーザパス.</param>
	/// <param name="onEndedCB">終了コールバック.</param>
	/// <param name="headers">拡張用ヘッダ.</param>
	/// <param name="fields">拡張用パラメータ.</param>
	/// <param name="postData">生Postデータ.</param>
	/// <returns></returns>
	public static IEnumerator Send( string url, string username, string pass, System.Action<WWW> onEndedCB, Dictionary<string,string> headers=null, Dictionary<string, object> fields =null, byte[] postData=null ) {

		// トークンを取る.
		string tokenURL = TOKEN_REST_URL;
		if ( url.Contains( "stream" ) ) tokenURL = TOKEN_STREAM_URL;
		string basicAuthStr = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(username + ":" + pass));
		var tokenHeaders = new Dictionary<string, string>();
		tokenHeaders.Add( "Authorization", basicAuthStr );
		WWW tokenWWW = new WWW( tokenURL + "?url="+url, null, tokenHeaders );
		while ( !tokenWWW.isDone && tokenWWW.error == null ) {
			yield return null;
		}
		if ( tokenWWW.error != null ) {
			Debug.LogError( "認証失敗, url,username,passwordのどれかが間違ってる可能性あり" );
			Debug.LogError( tokenWWW.error + " : " + tokenWWW.text );
			yield break;
		}

		var token = System.Text.Encoding.UTF8.GetString( tokenWWW.bytes );

		// フィールド群を整形.
		if ( fields == null ) fields = new Dictionary<string, object>( 1 );
		fields.Add( "watson-token", token );
		System.Text.StringBuilder sb = null;
		foreach ( var itr in fields ) {
			string now = null;
			if ( itr.Value is string ) now = ( itr.Key + "=" + WWW.EscapeURL( ( string )itr.Value ) );
			else if ( itr.Value is byte[] ) now = ( itr.Key + "=" + System.Convert.ToBase64String( ( byte[] )itr.Value ) );
			else now = ( itr.Key + "=" + itr.Value.ToString() );

			if ( sb == null )	sb = new System.Text.StringBuilder( 10000 );
			else				sb.Append( "&" );
			sb.Append( now );
		}

		// ヘッダ整備.
		if ( headers == null ) headers = new Dictionary<string, string>( 1 );
		headers[ "X-Watson-Authorization-Token" ] = token;

		// 実行.
		string nowURL = string.Format( "{0}?{1}", url, sb.ToString() );
		var ret = new WWW( nowURL, postData, headers );
		while ( !ret.isDone && ret.error == null ) {
			yield return null;
		}

		if ( ret.error != null ) {
			Debug.LogError( string.Format( "また今回も駄目だったよ, {0} は言うことを聞かないからな。", url ) );
			Debug.LogError( string.Format( "神は言っている, {0} {1} と。", ret.error, ret.text ) );
		}

		onEndedCB( ret );
	}
}
