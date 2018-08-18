using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// フィールドクラス
/// </summary>
public class Field
{
	//! マスのコスト
	public int[][] _cells = null;
	//! フィールドのサイズ
	public Vector2Int size { get; set; } = Vector2Int.zero;

	/// <summary>
	/// ランダムなコストのフィールドを作成する
	/// </summary>
	/// <param name="size"></param>
	public static Field CreateRandomField( Vector2Int size )
	{
		var field = new Field();
		field.size = size;

		field._cells = new int[ size.y ][];
		for( int y = 0; y < size.y; y ++ )
		{
			field._cells[ y ] = new int[ size.x ];
			for(int x = 0; x < size.x; x ++ )
			{
				field._cells[ y ][ x ] = Random.Range( 1, 10 );
			}
		}
		return field;
	}

	/// <summary>
	/// セル取得
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public int GetCell( Vector2Int position )
	{
		return _cells[ position.y ][ position.x ];
	}
}

static public class Utility
{
	public static readonly Vector2Int[] Direction =
	{
		Vector2Int.down,
		Vector2Int.up,
		Vector2Int.left,
		Vector2Int.right,
	};

	public static int ToIndex( Vector2Int position, int size_x )
	{
		return position.y * size_x + position.x;
	}

	public static Vector2Int ToPosition( int index, int size_x )
	{
		return new Vector2Int( index % size_x, index / size_x );
	}

	public static bool IsOut( Vector2Int position, Vector2Int size )
	{
		if( position.x < 0 || position.y < 0 || position.x >= size.x || position.y >= size.y )
		{
			return true;
		}
		return false;
	}
}