using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RouteTester : MonoBehaviour
{
	//! コンテキスト
	const int MAIN_THREAD_ROUTER = 0;
	const int JOB_SYSTEM_ROUTER = 1;

	//! 経路探索コンテキスト
	IRouter[] _router = new IRouter[ 2 ];

	//! フィールド
	Field _field = null;

	//! スタート
	Vector2Int _start = Vector2Int.zero;
	//! ゴール
	Vector2Int[] _goals = null;

	//! UI
	[SerializeField]
	Button _button_mainthread = null;
	[SerializeField]
	Button _button_jobsystem = null;
	[SerializeField]
	Text _text_log = null;

	//! フィールド
	[SerializeField]
	Transform _field_parent = null;
	[SerializeField]
	GameObject _prefab_cell = null;
	[SerializeField]
	Vector2 _cell_size = new Vector2( 20f, 20f );
	[SerializeField]
	uint _goal_num = 10;
	[SerializeField]
	Vector2Int _field_size = new Vector2Int( 40, 40 );
	[SerializeField]
	uint _tries_test = 10;

	void Start()
	{
		_router[ MAIN_THREAD_ROUTER ] = new MainThreadRouter();
		_router[ JOB_SYSTEM_ROUTER ] = new JobSystemRouter();

		// ボタンコールバック
		_button_mainthread.onClick.AddListener( () =>
		{
			StartCoroutine( TestRouting( _router[ MAIN_THREAD_ROUTER ] ) );
		} );
		_button_jobsystem.onClick.AddListener( () =>
		{
			StartCoroutine( TestRouting( _router[ JOB_SYSTEM_ROUTER ] ) );
		} );
	}

	/// <summary>
	/// フィールドリセット
	/// </summary>
	void ResetField()
	{
		// セルをクリア
		List<Transform> cells = new List<Transform>( _field_parent.childCount );
		for ( int i = 0; i < _field_parent.childCount; i++ )
		{
			cells.Add( _field_parent.GetChild( i ) );
		}
		foreach( var cell in cells )
		{
			Destroy( cell.gameObject );
		}

		// フィールド初期化
		_field = Field.CreateRandomField( _field_size );

		// ゴールとスタートの候補地リスト
		List<Vector2Int> position_candidate = new List<Vector2Int>( _field_size.x * _field_size.y );

		// セルの作成と候補地のセット
		for ( int y = 0; y < _field.size.y; y++ )
		{
			for ( int x = 0; x < _field.size.x; x++ )
			{
				Vector2Int cell_position = new Vector2Int( x, y );
				var color = Color.HSVToRGB( 0.25f - _field.GetCell( cell_position ) / 40f, 0.5f, 1f );
				CreateCell( cell_position, color );

				position_candidate.Add( cell_position );
			}
		}

		// 候補地をシャッフル
		for ( int i = 0; i < position_candidate.Count; i++ )
		{
			int j = Random.Range( i, position_candidate.Count );
			Vector2Int temp = position_candidate[ i ];
			position_candidate[ i ] = position_candidate[ j ];
			position_candidate[ j ] = temp;
		}

		// ゴールとスタートに候補地を設定
		_goals = new Vector2Int[ _goal_num ];
		int candidate_index = 0;
		for ( int i = 0; i < _goals.Length; i++ )
		{
			_goals[ i ] = position_candidate[ candidate_index ];

			CreateCell( _goals[ i ], Color.blue );

			candidate_index++;
		}
		_start = position_candidate[ candidate_index ];
		CreateCell( _start, Color.red );
	}

	/// <summary>
	/// ルート探索開始
	/// </summary>
	/// <param name="router"></param>
	/// <param name="field"></param>
	float StartRouting( IRouter router, Field field, Vector2Int start, Vector2Int[] goals )
	{
		Debug.Log("経路探索開始");
		var stopwatch = new System.Diagnostics.Stopwatch();

		stopwatch.Start();
		var paths = router.FindPath( field, start, goals );
		stopwatch.Stop();
		float elapsed_time = stopwatch.ElapsedMilliseconds;
		Debug.Log( "経路探索終了 経過時間:" + elapsed_time + "ms" );

		// 経路表示
		for( int i = 0; i < paths.Length; i ++ )
		{
			DispPath( paths[ i ] );
		}

		return elapsed_time;
	}

	IEnumerator TestRouting( IRouter router )
	{
		string log = "開始[" + router.ToString() + "]\n";
		float total_time = 0f;
		for ( int i = 0; i < _tries_test; i++ )
		{
			ResetField();

			float elapsed_time = StartRouting( router, _field, _start, _goals );
			total_time += elapsed_time;

			log += string.Format( "{0}:{1}ms\n", i + 1, elapsed_time );
			_text_log.text = log;

			yield return new WaitForSeconds( 0.5f );
		}

		log += "平均時間 :" + ( total_time / _tries_test ) + "ms\n";
		_text_log.text = log;
	}

	/// <summary>
	/// パス描画
	/// </summary>
	/// <param name="route"></param>
	void DispPath( Route route )
	{
		for( int i = 0; i < route.path.Length; i ++ )
		{
			CreatePath( route.path[ i ], Color.cyan );
		}
	}

	/// <summary>
	/// セルの作成
	/// </summary>
	/// <param name="position"></param>
	/// <param name="color"></param>
	void CreateCell( Vector2Int position, Color color )
	{
		GameObject cell = Instantiate( _prefab_cell, _field_parent, false );
		cell.transform.localPosition = new Vector3( position.x * _cell_size.x, position.y * _cell_size.y, 0f ) + new Vector3( -400f, -400f, 0f );
		var image = cell.GetComponent<Image>();

		image.color = color;
	}

	/// <summary>
	/// セルの作成
	/// </summary>
	/// <param name="position"></param>
	/// <param name="color"></param>
	void CreatePath( Vector2Int position, Color color )
	{
		GameObject cell = Instantiate( _prefab_cell, _field_parent, false );
		cell.transform.localPosition = new Vector3( position.x * _cell_size.x, position.y * _cell_size.y, 0f ) + new Vector3( -400f, -400f, 0f ) + new Vector3( _cell_size.x, _cell_size.y, 0f ) * 0.5f;
		cell.transform.localScale = Vector3.one * 0.5f;
		var image = cell.GetComponent<Image>();

		image.color = color;
	}
}
