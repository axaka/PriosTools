/*
 * Author: Trung Dong
 * www.trung-dong.com
 * Last update: 2018/01/21
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
*/

/*
 * Changes by Martin Vadseth Høiby
 * Added prettyJson option and changed file type to .json
 * Added .cs class generator for the json file
 */
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System.Collections;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using Newtonsoft.Json;

using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Linq;

namespace PriosTools
{

	public class GSpreadSheetsToJson : EditorWindow
	{

		static string CLIENT_ID = "871414866606-7b9687cp1ibjokihbbfl6nrjr94j14o8.apps.googleusercontent.com";

		static string CLIENT_SECRET = "zF_J3qHpzX5e8i2V-ZEvOdGV";

		static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };

		/// <summary>
		/// Key of the spreadsheet. Get from url of the spreadsheet.
		/// </summary>
		[SerializeField]
		private string spreadsheetURL = "";
		private string spreadsheetKey => ExtractSpreadsheetKey(spreadsheetURL);

		/// <summary>
		/// List of sheet names which want to download and convert to json file
		/// </summary>
		[SerializeField]
		private List<string> wantedSheetNames = new List<string>();

		/// <summary>
		/// Should the json file be made more human readable?
		/// </summary>
		[SerializeField]
		private bool prettyJson = true;

		/// <summary>
		/// Should matching class file be created?
		/// </summary>
		[SerializeField]
		private bool createClassFile = true;

		/// <summary>
		/// Name of application.
		/// </summary>
		private string appName = "Unity";

		/// <summary>
		/// The directory which contain json files.
		/// </summary>
		[SerializeField]
		private string jsonDir = "./Assets/Resources/JsonData/";

		/// <summary>
		/// The directory which contain class files.
		/// </summary>
		[SerializeField]
		private string classDir = "./Assets/Scripts/JsonClass/";

		/// <summary>
		/// The data types which is allowed to convert from sheet to json object
		/// </summary>
		private static List<string> baseTypes = new List<string>() { "string", "int", "bool", "float" };


		/// <summary>
		/// Position of the scroll view.
		/// </summary>
		private Vector2 scrollPosition;

		/// <summary>
		/// Progress of download and convert action. 100 is "completed".
		/// </summary>
		private float progress = 100;
		/// <summary>
		/// The message which be shown on progress bar when action is running.
		/// </summary>
		private string progressMessage = "";

		[MenuItem("Utility/GSheet to Json")]
		private static void ShowWindow()
		{
			GSpreadSheetsToJson window = EditorWindow.GetWindow(typeof(GSpreadSheetsToJson)) as GSpreadSheetsToJson;
		}

		public void Init()
		{
			progress = 100;
			progressMessage = "";
			ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
		}

		private void OnGUI()
		{
			Init();

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUI.skin.scrollView);
			GUILayout.BeginVertical();
			{
				GUILayout.Label("Settings", EditorStyles.boldLabel);
				spreadsheetURL = EditorGUILayout.TextField("Spread sheet URL", spreadsheetURL);
				jsonDir = EditorGUILayout.TextField("Path to store json files", jsonDir);
				classDir = EditorGUILayout.TextField("Path to store class files", classDir);
				prettyJson = EditorGUILayout.Toggle("Pretty Json", prettyJson);
				createClassFile = EditorGUILayout.Toggle("Create matching classes", createClassFile);
				GUILayout.Label("");
				GUILayout.Label("Sheet names", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("These sheets below will be downloaded. Let the list blank (remove all items) if you want to download all sheets", MessageType.Info);

				int _removeId = -1;
				for (int i = 0; i < wantedSheetNames.Count; i++)
				{
					GUILayout.BeginHorizontal();
					wantedSheetNames[i] = EditorGUILayout.TextField(string.Format("Sheet {0}", i), wantedSheetNames[i]);
					if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
					{
						_removeId = i;
					}
					GUILayout.EndHorizontal();
				}
				if (_removeId >= 0)
					wantedSheetNames.RemoveAt(_removeId);
				if (wantedSheetNames.Count <= 0)
				{
					GUILayout.Label("Download all sheets");
				}
				else
					GUILayout.Label(string.Format("Download {0} sheets", wantedSheetNames.Count));
				if (GUILayout.Button("Add sheet name", GUILayout.Width(130)))
				{
					wantedSheetNames.Add("");
				}
				GUILayout.Label("");
				GUI.backgroundColor = UnityEngine.Color.green;
				if (GUILayout.Button("Download data \nthen convert to Json files"))
				{
					progress = 0;
					DownloadToJson();
				}
				GUI.backgroundColor = UnityEngine.Color.white;
				if ((progress < 100) && (progress > 0))
				{
					if (EditorUtility.DisplayCancelableProgressBar("Processing", progressMessage, progress / 100))
					{
						progress = 100;
						EditorUtility.ClearProgressBar();
					}
				}
				else
				{
					EditorUtility.ClearProgressBar();
				}
			}

			try
			{
				GUILayout.EndVertical();
				EditorGUILayout.EndScrollView();
			}
			catch (Exception)
			{
				//Sometimes, Unity fire a "InvalidOperationException: Stack empty." bug when Editor want to end a group layout
			}
		}

		public static string ExtractSpreadsheetKey(string url)
		{
			var match = System.Text.RegularExpressions.Regex.Match(url, @"\/d\/([a-zA-Z0-9-_]+)");
			if (match.Success && match.Groups.Count > 1)
			{
				return match.Groups[1].Value;
			}
			else
			{
				Console.WriteLine("Invalid Google Sheets URL.");
				return null;
			}
		}


		private void DownloadToJson()
		{
			//Validate input
			if (string.IsNullOrEmpty(spreadsheetKey))
			{
				Debug.LogError($"{nameof(spreadsheetKey)} cannot be null!");
				return;
			}

			Debug.Log("Start downloading from: " + spreadsheetURL);

			//Authenticate
			progressMessage = "Authenticating...";
			var service = new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = GetCredential(),
				ApplicationName = appName,
			});

			progress = 5;
			EditorUtility.DisplayCancelableProgressBar("Processing", progressMessage, progress / 100);
			progressMessage = "Get list of spreadsheets...";
			EditorUtility.DisplayCancelableProgressBar("Processing", progressMessage, progress / 100);

			Spreadsheet spreadsheetData = service.Spreadsheets.Get(spreadsheetKey).Execute();
			IList<Sheet> sheets = spreadsheetData.Sheets;


			//if((feed == null)||(feed.Entries.Count <= 0))
			if ((sheets == null) || (sheets.Count <= 0))
			{
				Debug.LogError("Not found any data!");
				progress = 100;
				EditorUtility.ClearProgressBar();
				return;
			}

			progress = 15;

			//For each sheet in received data, check the sheet name. If that sheet is the wanted sheet, add it into the ranges.
			List<string> ranges = new List<string>();
			foreach (Sheet sheet in sheets)
			{
				if ((wantedSheetNames.Count <= 0) || (wantedSheetNames.Contains(sheet.Properties.Title)))
				{
					ranges.Add(sheet.Properties.Title);
				}
			}

			SpreadsheetsResource.ValuesResource.BatchGetRequest request = service.Spreadsheets.Values.BatchGet(spreadsheetKey);
			request.Ranges = ranges;
			BatchGetValuesResponse response = request.Execute();

			//For each wanted sheet, create a json file
			foreach (ValueRange valueRange in response.ValueRanges)
			{
				string Sheetname = valueRange.Range.Split('!')[0];
				progressMessage = string.Format("Processing {0}...", Sheetname);
				EditorUtility.DisplayCancelableProgressBar("Processing", progressMessage, progress / 100);

				string[][] sheetData = valueRange.Values
					.Select(row => row.Select(cell => cell?.ToString() ?? "").ToArray())
					.ToArray();


				string jsonFilePath = Path.Combine(jsonDir, Sheetname + ".json");
				CreateJsonFile(jsonFilePath, sheetData);

				if (createClassFile)
				{
					string classFilePath = Path.Combine(classDir, Sheetname + ".cs");

					CreateClassFile(jsonFilePath, Sheetname, classFilePath);
				}

				if (wantedSheetNames.Count <= 0)
					progress += 85 / (response.ValueRanges.Count);
				else
					progress += 85 / wantedSheetNames.Count;
			}
			progress = 100;
			AssetDatabase.Refresh();

			Debug.Log("Download completed.");
		}

		private void CreateClassFile(string jsonPath, string className, string outputPath)
		{
			// Ensure the output directory exists
			string directory = Path.GetDirectoryName(outputPath);
			Directory.CreateDirectory(directory);

			// Create class file
			JsonToCSharpGenerator.GenerateCSharpClass(jsonPath, className, outputPath);
		}

		private static object ParseSingleValue(string type, string value)
		{
			return type switch
			{
				"string" => value ?? "",
				"int" => string.IsNullOrEmpty(value) ? 0 : int.Parse(value),
				"float" => string.IsNullOrEmpty(value) ? 0f : float.Parse(value),
				"bool" => string.IsNullOrEmpty(value) ? false : bool.Parse(value),
				_ => throw new ArgumentException($"Unsupported type: {type}"),
			};
		}

		private static object ParseArrayValue(string type, string value, string separator)
		{
			if (string.IsNullOrWhiteSpace(value)) return Array.Empty<object>();

			string[] parts = value.Split(new string[] { separator }, StringSplitOptions.None);

			return type switch
			{
				"string" => parts.Select(s => s.Trim()).ToArray(),
				"int" => parts.Select(s => string.IsNullOrWhiteSpace(s) ? 0 : int.Parse(s.Trim())).ToArray(),
				"float" => parts.Select(s => string.IsNullOrWhiteSpace(s) ? 0f : float.Parse(s.Trim())).ToArray(),
				"bool" => parts.Select(s => string.IsNullOrWhiteSpace(s) ? false : bool.Parse(s.Trim())).ToArray(),
				_ => throw new ArgumentException($"Unsupported array base type: {type}"),
			};
		}


		public static void CreateJsonFile(string filePath, string[][] sheetData)
		{
			if (sheetData.Length < 2)
			{
				Debug.LogWarning($"Sheet '{filePath}' does not contain enough rows to parse headers and types.");
				return;
			}

			string[] propertyNames = sheetData[0];
			string[] dataTypes = sheetData[1];
			var jsonDataList = new List<Dictionary<string, object>>();

			for (int rowId = 2; rowId < sheetData.Length; rowId++)
			{
				var thisRow = sheetData[rowId];
				var data = new Dictionary<string, object>();
				bool thisRowHasError = false;

				for (int columnId = 0; columnId < propertyNames.Length; columnId++)
				{
					string propertyName = propertyNames[columnId].Trim();
					if (string.IsNullOrEmpty(propertyName)) continue;

					string dataType = dataTypes[columnId].Trim();
					string strVal = columnId < thisRow.Length ? thisRow[columnId].Trim() : string.Empty;

					try
					{
						if (baseTypes.Contains(dataType))
						{
							data[propertyName] = ParseSingleValue(dataType, strVal);
						}
						else if (dataType.Contains("[") && dataType.EndsWith("]"))
						{
							string baseType = dataType[..dataType.IndexOf("[")];
							string separator = dataType[(dataType.IndexOf("[") + 1)..^1]; // extract inside brackets

							if (!baseTypes.Contains(baseType))
							{
								Debug.LogWarning($"Unsupported array base type: {baseType} in {filePath}");
								thisRowHasError = true;
								break;
							}

							object parsedArray = ParseArrayValue(baseType, strVal, separator);
							data[propertyName] = parsedArray;
						}
						else
						{
							Debug.LogWarning($"Unsupported type format: {dataType} in {filePath}");
							thisRowHasError = true;
							break;
						}
					}
					catch (Exception e)
					{
						Debug.LogError($"Error parsing row {rowId + 1}, column '{propertyName}' in {filePath}: {e.Message}");
						thisRowHasError = true;
						break;
					}
				}

				if (!thisRowHasError)
				{
					jsonDataList.Add(data);
				}
			}

			string jsonString = JsonConvert.SerializeObject(jsonDataList, Formatting.Indented);
			try
			{
				File.WriteAllText(filePath, jsonString);
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to write JSON file at {filePath}: {e.Message}");
			}

		}

		UserCredential GetCredential()
		{
			MonoScript ms = MonoScript.FromScriptableObject(this);
			string scriptFilePath = AssetDatabase.GetAssetPath(ms);
			FileInfo fi = new FileInfo(scriptFilePath);
			string scriptFolder = fi.Directory.ToString();
			scriptFolder.Replace('\\', '/');
			Debug.Log("Save Credential to: " + scriptFolder);

			UserCredential credential = null;
			ClientSecrets clientSecrets = new ClientSecrets();
			clientSecrets.ClientId = CLIENT_ID;
			clientSecrets.ClientSecret = CLIENT_SECRET;
			try
			{
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					clientSecrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore(scriptFolder, true)).Result;
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}

			return credential;
		}

		public bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			bool isOk = true;
			// If there are errors in the certificate chain, look at each error to determine the cause.
			if (sslPolicyErrors != SslPolicyErrors.None)
			{
				for (int i = 0; i < chain.ChainStatus.Length; i++)
				{
					if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
					{
						chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
						chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
						chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
						chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
						bool chainIsValid = chain.Build((X509Certificate2)certificate);
						if (!chainIsValid)
						{
							Debug.LogError("certificate chain is not valid");
							isOk = false;
						}
					}
				}
			}
			return isOk;
		}
	}
}