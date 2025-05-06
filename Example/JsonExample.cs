using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PriosTools
{
    public class JsonExample : MonoBehaviour
    {
        [SerializeField] private TMP_Text _textField;

        void Start()
        {
            string outputText = "";
			List<Test> items = Test.LoadJson();
            foreach (var item in items)
            {
                outputText += $"{item.Var1} - {item.Var2}\n";
			}
            _textField.text = outputText;
		}
    }
}
