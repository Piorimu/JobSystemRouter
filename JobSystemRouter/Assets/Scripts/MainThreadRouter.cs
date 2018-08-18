using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadRouter : IRouter
{
	const int STATUS_FREE = 0;      //!< 未探索
	const int STATUS_OPEN = 1;      //!< オープン
	const int STATUS_CLOSE = 2; //!< クローズ

	const int MAX_WALKS = 1000;

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

	/// <summary>
	/// パス探索
	/// </summary>
	public Route[] FindPath( Field field, Vector2Int start, Vector2Int[] goals )
	{
		var paths = new Route[ goals.Length ];
		for ( int i = 0; i < goals.Length; i++ )
		{
			paths[ i ] = FindPath( field, start, goals[ i ] );
		}
		return paths;

	}

	/// <summary>
	/// パス探索(1ゴール)
	/// </summary>
	/// <returns></returns>
	Route FindPath( Field field, Vector2Int start, Vector2Int goal )
	{
		Node[] nodes = new Node[ field.size.x * field.size.y ];

		// スタートとゴールをマーク
		int start_index = Utility.ToIndex( start, field.size.x );
		int goal_index = Utility.ToIndex( goal, field.size.x );
		nodes[ start_index ].status = STATUS_OPEN;
		nodes[ goal_index ].parentIndex = -1;

		int tries = 0;
		for ( ; tries < MAX_TRIES; tries ++ )
		{
			// 最小スコアのノードを選択
			int node_index = -1;
			int min_score = int.MaxValue;
			for ( int i = 0; i < field.size.y * field.size.x; i++ )
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

			OpenNode( field, nodes, node_index, goal );
		}

		if( tries == MAX_TRIES )
		{
			Debug.Log("最大試行数到達");
		}

		// ゴールにたどり着けず
		if ( nodes[ goal_index ].parentIndex == -1 )
		{
			return null;
		}

		// ルート作成
		Vector2Int[] buffer_path = new Vector2Int[ MAX_WALKS ];
		int walks = 0;
		// ゴールからスタートまでの道のりをたどる
		for ( int index = goal_index; index != start_index; index = nodes[ index ].parentIndex )
		{
			buffer_path[ walks ] = Utility.ToPosition( index, field.size.x );
			walks++;
		}

		// 逆からたどればスタート→ゴール
		Route route = new Route();
		route.path = new Vector2Int[ walks ];
		for ( int i = 0; i < route.path.Length; i++ )
		{
			route.path[ i ] = buffer_path[ walks - i - 1 ];
		}
		return route;
	}

	/// <summary>
	/// ノードオープン
	/// </summary>
	/// <param name="field"></param>
	/// <param name="nodes"></param>
	/// <param name="node_index"></param>
	/// <param name="goal"></param>
	void OpenNode( Field field, Node[] nodes, int node_index, Vector2Int goal )
	{
		// 添字から座標に
		Vector2Int center = Utility.ToPosition( node_index, field.size.x );

		int center_cost = nodes[ node_index ].cost;
		int center_score = nodes[ node_index ].score;

		for ( int i = 0; i < Utility.Direction.Length; i++ )
		{
			Vector2Int open_position = center + Utility.Direction[ i ];

			if ( Utility.IsOut( open_position, field.size ) )
			{
				continue;
			}

			// コスト計算
			int cost = field.GetCell( open_position ) + center_cost + 1;
			int heuristic = System.Math.Abs( goal.x - open_position.x ) + System.Math.Abs( goal.y - open_position.y );
			int score = cost + heuristic + 1;

			int next_index = Utility.ToIndex( open_position, field.size.x );
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
