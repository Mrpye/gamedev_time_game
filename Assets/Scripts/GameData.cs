using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class GameData : ScriptableObject, ISerializationCallbackReceiver {
	public int StartLevel;
	//public List<Vector3> spawnpoints = new List<Vector3>();

	
	[NonSerialized]
	public int CurrentLevel;

	[NonSerialized]
	public List<FileStream> fs = new List<FileStream>();
	

	public void IncLevel() {
		CurrentLevel++;
	}
	//public Vector3 GetNextStartPoint() {
		//return spawnpoints[CurrentLevel];
	//}

	public void OnAfterDeserialize() {
		CurrentLevel = StartLevel;
	}

	public void OnBeforeSerialize() { }
}