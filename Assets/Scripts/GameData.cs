using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu]
public class GameData : ScriptableObject, ISerializationCallbackReceiver {
	public int StartLevel;
	//public List<Vector3> spawnpoints = new List<Vector3>();

	//[NonSerialized]
	//public int current_level;

	//[NonSerialized]
	//public List<FileStream> fs = new List<FileStream>();
	//[NonSerialized]
	//public List<int> session_count = new List<int>();



	//[NonSerialized]
	//public float start_time = 0;

	

	//public void IncLevel() {
	//	current_level++;
	//}
	//public Vector3 GetNextStartPoint() {
	//return spawnpoints[CurrentLevel];
	//}

	public void OnAfterDeserialize() {
		//current_level = StartLevel;
	}

	public void OnBeforeSerialize() { }




}