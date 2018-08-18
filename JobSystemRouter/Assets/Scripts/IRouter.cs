using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ルート構造体
/// </summary>
public class Route
{
	//! 経路座標配列
	public Vector2Int[] path;
}

/// <summary>
/// 経路探索インターフェース
/// </summary>
interface IRouter
{
	/// <summary>
	/// パス探索
	/// </summary>
	Route[] FindPath( Field field, Vector2Int start, Vector2Int[] goals );
}