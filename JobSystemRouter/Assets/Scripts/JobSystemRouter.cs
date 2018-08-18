using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

/// <summary>
/// 経路探索ジョブ
/// </summary>
public struct JobRouter : IJob
{
	const int STATUS_FREE = 0;      //!< 未探索
	const int STATUS_OPEN = 1;      //!< オープン
	const int STATUS_CLOSE = 2; //!< クローズ

	// 配列確保数でも使うのでpublic
	public const int MAX_WALKS = 1000;

	const int MAX_TRIES = 100000;

	/// <summary>
	/// ノード
	/// </summary>
	struct Node
	{
		public int status;          //!< 状態
		public int parentIndex;     //!< 親の座標インデックス

		public int cost;                //!< コスト
		public int heuristic;           //!< ヒューリスティックコスト

		//! スコア計算プロパティ
		public int score
		{
			get { return cost + heuristic; }
		}
	}

	//! フィールド配列
	[ReadOnly]	// 共有する配列には、[ReadOnly]が必要
	public NativeArray<int> costs;
	//! フィールドサイズ
	public Vector2Int fieldSize;
	//! スタート地点
	public Vector2Int start;
	//! ゴール地点
	public Vector2Int goal;

	//! 経路出力配列
	public NativeArray<Vector2Int> resultPath;
	//! 歩数出力配列
	public NativeArray<int> resultWalks;

	/// <summary>
	/// 経路探索
	/// </summary>
	public void Execute()
	{
		// ジョブ内での確保は出来る
		Node[] nodes = new Node[ fieldSize.x * fieldSize.y ];

		// スタートとゴールをマーク
		int start_index = Utility.ToIndex( start, fieldSize.x );
		int goal_index = Utility.ToIndex( goal, fieldSize.x );
		nodes[ start_index ].status = STATUS_OPEN;
		nodes[ goal_index ].parentIndex = -1;

		int tries = 0;
		for ( ; tries < MAX_TRIES; tries++ )
		{
			// 最小スコアのノードを選択
			int node_index = -1;
			int min_score = int.MaxValue;
			for ( int i = 0; i < fieldSize.y * fieldSize.x; i++ )
			{
				// 開いていないならスキップ
				if ( nodes[ i ].status != STATUS_OPEN )
				{
					continue;
				}
				// よりスコアが低いノードを選択
				if ( nodes[ i ].score < min_score )
				{
					node_index = i;
					min_score = nodes[ i ].heuristic;
				}
			}
			// 開いたノードがなかった
			if ( node_index == -1 )
			{
				break;
			}

			OpenNode( nodes, node_index );
		}

		if ( tries == MAX_TRIES )
		{
			Debug.Log( "最大試行数到達" );
		}

		// ゴールにたどり着けず
		if ( nodes[ goal_index ].parentIndex == -1 )
		{
			resultWalks[ 0 ] = -1;
			return;
		}

		// ルート作成
		Vector2Int[] buffer_path = new Vector2Int[ MAX_WALKS ];
		int walks = 0;
		// ゴールからスタートまでの道のりをたどる
		for ( int index = goal_index; index != start_index; index = nodes[ index ].parentIndex )
		{
			buffer_path[ walks ] = Utility.ToPosition( index, fieldSize.x );
			walks++;
		}

		// 逆からたどればスタート→ゴール
		for ( int i = 0; i < walks; i++ )
		{
			resultPath[ i ] = buffer_path[ walks - i - 1 ];
		}
		resultWalks[ 0 ] = walks;
	}

	/// <summary>
	/// ノードオープン
	/// </summary>
	/// <param name="field"></param>
	/// <param name="nodes"></param>
	/// <param name="node_index"></param>
	/// <param name="goal"></param>
	void OpenNode( Node[] nodes, int node_index )
	{
		// 添字から座標に
		Vector2Int center = Utility.ToPosition( node_index, fieldSize.x );

		int center_cost = nodes[ node_index ].cost;
		int center_score = nodes[ node_index ].score;

		for ( int i = 0; i < Utility.Direction.Length; i++ )
		{
			Vector2Int open_position = center + Utility.Direction[ i ];

			if ( Utility.IsOut( open_position, new Vector2Int( fieldSize.x, fieldSize.y ) ) )
			{
				continue;
			}

			// コスト計算
			int cost = costs[ Utility.ToIndex( open_position, fieldSize.x ) ] + center_cost + 1;
			int heuristic = System.Math.Abs( goal.x - open_position.x ) + System.Math.Abs( goal.y - open_position.y );
			int score = cost + heuristic + 1;

			int next_index = Utility.ToIndex( open_position, fieldSize.x );
			if ( nodes[ next_index ].status == STATUS_FREE || nodes[ next_index ].score > score )
			{
				nodes[ next_index ].status = STATUS_OPEN;
				nodes[ next_index ].cost = cost;
				nodes[ next_index ].heuristic = heuristic;
				nodes[ next_index ].parentIndex = node_index;
			}
		}
		nodes[ node_index ].status = STATUS_CLOSE;
	}
}

/// <summary>
/// 経路探索ジョブテスト
/// </summary>
public class JobSystemRouter : IRouter
{
	public Route[] FindPath( Field field, Vector2Int start, Vector2Int[] goals )
	{
		// ゴールの数だけジョブを回す
		JobHandle[] job_handles = new JobHandle[ goals.Length ];
		NativeArray<Vector2Int>[] result_paths = new NativeArray<Vector2Int>[ goals.Length ];
		NativeArray<int>[] result_walks = new NativeArray<int>[ goals.Length ];

		// フィールドのコストをNativeArrayに
		NativeArray<int> field_cost = new NativeArray<int>( field.size.x * field.size.y, Allocator.Temp );
		for( int i = 0; i < field.size.x * field.size.y; i ++ )
		{
			field_cost[ i ] = field.GetCell( Utility.ToPosition( i, field.size.x ) );
		}

		for( int i = 0; i < goals.Length; i ++ )
		{
			result_paths[ i ] = new NativeArray<Vector2Int>( JobRouter.MAX_WALKS, Allocator.Temp );
			result_walks[ i ] = new NativeArray<int>( 1, Allocator.Temp );

			// ジョブを作成
			var job_router = new JobRouter()
			{	// コンストラクタで各種情報を設定
				costs = field_cost,
				fieldSize = field.size,
				goal = goals[ i ],
				start = start,
				resultPath = result_paths[ i ],
				resultWalks = result_walks[ i ],
			};

			// ジョブをスケジュール
			job_handles[ i ] = job_router.Schedule();
		}
		// ジョブを開始
		JobHandle.ScheduleBatchedJobs();
		// 順番に経路探索ジョブを待って、結果を作成
		Route[] results = new Route[ goals.Length ];
		for( int i = 0; i < goals.Length; i ++ )
		{
			// ジョブ待ち
			job_handles[ i ].Complete();

			// 結果をルートに変換
			var route = new Route();
			route.path = new Vector2Int[ result_walks[ i ][ 0 ] ];
			for( int j = 0; j < route.path.Length; j ++ )
			{
				route.path[ j ] = result_paths[ i ][ j ];
			}
			results[ i ] = route;

			result_paths[ i ].Dispose();
			result_walks[ i ].Dispose();
		}

		field_cost.Dispose();

		return results;
	}
}
