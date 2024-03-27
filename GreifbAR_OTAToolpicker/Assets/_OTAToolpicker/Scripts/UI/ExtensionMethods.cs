// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
using System;

namespace NMY.OTAToolpicker {
public static class ExtensionMethods
{
    public static string TPM_MarkupBullets(this string rawText, char findBullet = '•', float indent = 1f, float leftMargin = 1f ) 
    {
        bool isInBullet = false;
        string markedup = String.Format("<margin-left={0}em>", leftMargin);
        string indentLeftTag = String.Format("<indent={0}em>", indent), indentRightTag = "</indent>";
        foreach (char c in rawText) {
            if (c == findBullet) { markedup += findBullet + indentLeftTag; isInBullet = true; }
            else if (isInBullet && c == (char)10) { markedup += indentRightTag + c ; isInBullet = false;  }
            else { markedup += c; }
        }
        if (isInBullet) markedup += indentRightTag;
        markedup += "</margin>";
        return markedup;
    }
}

}