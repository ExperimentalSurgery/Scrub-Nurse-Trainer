using UnityEngine;

// HSV color model to be able to lerp colors (Color.Lerp doesn't make sense)
// Source: http://www.unifycommunity.com/wiki/index.php?title=HSBColor
// Author: Jonathan Czeck (aarku)
// Adapted by: Wolfram Kresse

namespace NMY {

[System.Serializable]
public struct HSVColor
{
    public float h; // range [0..1]
    public float s; // range [0..1]
    public float v; // range [0..1]
    public float a; // range [0..1]
	private static HSVColor retHSV; // it's a struct, so this should be ok...
	private static Color retCol; // it's a struct, so this should be ok...

    public HSVColor(float h, float s, float v, float a)
    {
        this.h = h;
        this.s = s;
        this.v = v;
        this.a = a;
    }
    
    public HSVColor(float h, float s, float v)
    {
        this.h = h;
        this.s = s;
        this.v = v;
        this.a = 1f;
    }
    
    public HSVColor(Color col)
    {
        HSVColor temp = FromColor(col);
        h = temp.h;
        s = temp.s;
        v = temp.v;
        a = temp.a;
    }
    
    public static HSVColor FromColor(Color color)
    {
        float r = color.r;
        float g = color.g;
        float b = color.b;

        float max = Mathf.Max(r, Mathf.Max(g, b));

        if (max <= 0)
        {
			retHSV.h=
			retHSV.s=
			retHSV.v=0;
			retHSV.a=color.a;
            return retHSV;
        }

        float min = Mathf.Min(r, Mathf.Min(g, b));
        float dif = max - min;

        if (max > min)
        {
            if (g == max)
            {
                retHSV.h = (b - r) / dif + 2f;
            }
            else if (b == max)
            {
                retHSV.h = (r - g) / dif + 4f;
            }
            else
            {
                retHSV.h = (g - b) / dif;
            }
            if (retHSV.h < 0)
            {
                retHSV.h = retHSV.h + 6f;
            }
        }
        else
        {
            retHSV.h = 0;
        }

        retHSV.h /= 6f;
        retHSV.s = dif / max;
        retHSV.v = max;
        retHSV.a = color.a;

        return retHSV;
    }

    public static Color ToColor(HSVColor hsvColor)
    {
        float r = hsvColor.v;
        float g = hsvColor.v;
        float b = hsvColor.v;
        if (hsvColor.s != 0)
        {
            float max = hsvColor.v;
            float dif = hsvColor.v * hsvColor.s;
            float min = hsvColor.v - dif;

            float h = hsvColor.h * 6f;

            if (h < 1f)
            {
                r = max;
                g = h * dif + min;
                b = min;
            }
            else if (h < 2f)
            {
                r = -(h - 2f) * dif + min;
                g = max;
                b = min;
            }
            else if (h < 3f)
            {
                r = min;
                g = max;
                b = (h - 2f) * dif + min;
            }
            else if (h < 4f)
            {
                r = min;
                g = -(h - 4f) * dif + min;
                b = max;
            }
            else if (h < 5f)
            {
                r = (h - 4f) * dif + min;
                g = min;
                b = max;
            }
            else
            {
                r = max;
                g = min;
                b = -(h - 6f) * dif + min;
            }
        }

		retCol.r=Mathf.Clamp01(r);
		retCol.g=Mathf.Clamp01(g);
		retCol.b=Mathf.Clamp01(b);
		retCol.a=hsvColor.a;
        return retCol;
    }

    public Color ToColor()
    {
        return ToColor(this);
    }
    
    public override string ToString()
    {
        return "H:" + h + " S:" + s + " V:" + v + " A:" + a;
    }
    
    public static HSVColor Lerp(HSVColor a, HSVColor b, float t)
    {
		float h,s;

		//check special case black: interpolate neither hue nor saturation!
		//check special case grey: don't interpolate hue!
		if(a.v==0){
			h=b.h;
			s=b.s;
		}else if(b.v==0){
			h=a.h;
			s=a.s;
		}else{
			if(a.s==0){
				h=b.h;
			}else if(b.s==0){
				h=a.h;
			}else{
				h=Mathf.LerpAngle(a.h*360f,b.h*360f,t)/360f;
				while (h < 0f)
					h += 1f;
				while (h > 1f)
					h -= 1f;
			}
			s=Mathf.Lerp(a.s,b.s,t);
		}
        return new HSVColor(h, s, Mathf.Lerp(a.v, b.v, t), Mathf.Lerp(a.a, b.a, t));
    }

    public static Color Slerp(Color a, Color b, float t) {
        return (HSVColor.Lerp(HSVColor.FromColor(a), HSVColor.FromColor(b), t)).ToColor();
    }

    public static void Test()
    {
        HSVColor color;
        
        color = new HSVColor(Color.red);
        Debug.Log("red: " + color);
        
        color = new HSVColor(Color.green);
        Debug.Log("green: " + color);
        
        color = new HSVColor(Color.blue);
        Debug.Log("blue: " + color);
        
        color = new HSVColor(Color.grey);
        Debug.Log("grey: " + color);
        
        color = new HSVColor(Color.white);
        Debug.Log("white: " + color);
        
        color = new HSVColor(new Color(0.4f, 1f, 0.84f, 1f));
        Debug.Log("0.4, 1f, 0.84: " + color);
        
        Debug.Log("164,82,84   .... 0.643137f, 0.321568f, 0.329411f  :" + ToColor(new HSVColor(new Color(0.643137f, 0.321568f, 0.329411f))));
    }
}

public static class ColorExtensions
{
    public static Color Slerp(this Color a, Color b, float t)
    {
        return (HSVColor.Lerp(HSVColor.FromColor(a), HSVColor.FromColor(b), t)).ToColor();
    }
}

}