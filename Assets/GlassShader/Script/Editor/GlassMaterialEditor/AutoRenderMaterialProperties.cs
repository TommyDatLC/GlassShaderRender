// Place this file in an Editor folder: Assets/Editor/AutoRenderMaterialProperties.cs

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GlassShader.Script.Editor.GlassMaterialEditor
{
    public static class AutoRenderMaterialProperties
    {
        public static void Execute(Material material, VisualElement root)
        {
            if (material == null || root == null)
            {
                Debug.LogWarning("AutoRenderMaterialProperties.Execute: material or root is null.");
                return;
            }

            var shader = material.shader;
            if (shader == null)
            {
                Debug.LogWarning("AutoRenderMaterialProperties.Execute: material has no shader.");
                return;
            }
        
            VisualElement currentContainer = root;

            Action<VisualElement> styleChild = (ve) =>
            {
                ve.style.fontSize = 14;
                ve.style.unityFontStyleAndWeight = FontStyle.Normal;
                ve.style.flexShrink = 0;
            };

            int propCount = shader.GetPropertyCount();
            for (int i = 0; i < propCount; ++i)
            {
                var flags = shader.GetPropertyFlags(i);
                if ((flags & UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector) != 0)
                    continue;

                string propName = shader.GetPropertyName(i);
                string displayName = shader.GetPropertyDescription(i);
                var propType = shader.GetPropertyType(i);
                string[] attributes = shader.GetPropertyAttributes(i) ?? Array.Empty<string>();

                var headerAttr = attributes.FirstOrDefault(a => a.StartsWith("Header(", StringComparison.OrdinalIgnoreCase) || a.Equals("Header", StringComparison.OrdinalIgnoreCase));
                if (headerAttr != null)
                {
                    string headerText = ParseAttributeArg(headerAttr) ?? displayName ?? propName;
                    var fold = new Foldout() { text = headerText };
                    fold.AddToClassList("Foldout");
                    fold.style.unityFontStyleAndWeight = FontStyle.Bold;
                    fold.style.unityTextAlign = TextAnchor.MiddleCenter;
                    root.Add(fold);
                    currentContainer = fold;
                
                }

             
                switch (propType)
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                    {
                        var obj = new ObjectField(displayName) { objectType = typeof(Texture), value = material.GetTexture(propName) };
                        obj.RegisterValueChangedCallback(evt =>
                        {
                            material.SetTexture(propName, evt.newValue as Texture);
                            EditorUtility.SetDirty(material);
                        });
                        styleChild(obj);
                        currentContainer.Add(obj);

                        bool noScaleOffset = attributes.Any(a => a.IndexOf("NoScaleOffset", StringComparison.OrdinalIgnoreCase) >= 0);
                        if (!noScaleOffset)
                        {
                            var tile = new Vector2Field("Tiling") { value = material.GetTextureScale(propName) };
                            tile.RegisterValueChangedCallback(evt =>
                            {
                                material.SetTextureScale(propName, evt.newValue);
                                EditorUtility.SetDirty(material);
                            });
                            styleChild(tile);
                            currentContainer.Add(tile);

                            var off = new Vector2Field("Offset") { value = material.GetTextureOffset(propName) };
                            off.RegisterValueChangedCallback(evt =>
                            {
                                material.SetTextureOffset(propName, evt.newValue);
                                EditorUtility.SetDirty(material);
                            });
                            styleChild(off);
                            currentContainer.Add(off);
                        }
                    }
                        break;

                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                    {
                        var col = new ColorField(displayName) { value = material.GetColor(propName) };
                        col.RegisterValueChangedCallback(evt =>
                        {
                            material.SetColor(propName, evt.newValue);
                            EditorUtility.SetDirty(material);
                        });
                        styleChild(col);
                        currentContainer.Add(col);
                    }
                        break;

                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                    {
                        var vec = new Vector4Field(displayName) { value = material.GetVector(propName) };
                        vec.RegisterValueChangedCallback(evt =>
                        {
                            material.SetVector(propName, evt.newValue);
                            EditorUtility.SetDirty(material);
                        });
                        styleChild(vec);
                        currentContainer.Add(vec);
                    }
                        break;

                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                    {
                        var keywordAttr = attributes.FirstOrDefault(a => a.StartsWith("KeywordEnum(", StringComparison.OrdinalIgnoreCase) || a.StartsWith("KeywordEnum", StringComparison.OrdinalIgnoreCase));
                        var enumAttr = attributes.FirstOrDefault(a => a.StartsWith("Enum(", StringComparison.OrdinalIgnoreCase) || a.StartsWith("Enum", StringComparison.OrdinalIgnoreCase));

                        if (keywordAttr != null || enumAttr != null)
                        {
                            string raw = ParseAttributeArg(keywordAttr ?? enumAttr) ?? "";
                            var entries = SplitCsv(raw);
                            if (entries.Count == 0)
                                entries = new List<string>() { "Option0", "Option1" };

                            var popup = new PopupField<string>(entries, 0) { label = displayName };
                            int currentIndex = Mathf.Clamp((int)material.GetFloat(propName), 0, entries.Count - 1);
                            popup.SetValueWithoutNotify(entries[currentIndex]);

                            popup.RegisterValueChangedCallback(evt =>
                            {
                                int selected = entries.IndexOf(evt.newValue);
                                material.SetFloat(propName, selected);
                                TryToggleKeywordSet(material, propName, entries, selected);
                                EditorUtility.SetDirty(material);
                            });

                            styleChild(popup);
                            currentContainer.Add(popup);
                            break;
                        }

                        if (propType == UnityEngine.Rendering.ShaderPropertyType.Range)
                        {
                            Vector2 limits = shader.GetPropertyRangeLimits(i);
                            var slider = new Slider(displayName, limits.x, limits.y) { value = material.GetFloat(propName) };
                            slider.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                            #region showInputField_safe
                            try
                            {
                                slider.showInputField = true;
                            }
                            catch { /* older versions may not support, ignore */ }
                            #endregion
                            slider.RegisterValueChangedCallback(evt =>
                            {
                                material.SetFloat(propName, evt.newValue);
                                EditorUtility.SetDirty(material);
                            });
                            styleChild(slider);
                            currentContainer.Add(slider);
                        }
                        else
                        {
                            var floatField = new FloatField(displayName) { value = material.GetFloat(propName) };
                            floatField.RegisterValueChangedCallback(evt =>
                            {
                                material.SetFloat(propName, evt.newValue);
                                EditorUtility.SetDirty(material);
                            });
                            styleChild(floatField);
                            currentContainer.Add(floatField);
                        }
                    }
                        break;

                    case UnityEngine.Rendering.ShaderPropertyType.Int:
                    {
                        // first check for KeywordEnum/Enum attributes on int
                        var keywordAttrInt = attributes.FirstOrDefault(a => a.StartsWith("KeywordEnum(", StringComparison.OrdinalIgnoreCase) || a.StartsWith("KeywordEnum", StringComparison.OrdinalIgnoreCase));
                        var enumAttrInt = attributes.FirstOrDefault(a => a.StartsWith("Enum(", StringComparison.OrdinalIgnoreCase) || a.StartsWith("Enum", StringComparison.OrdinalIgnoreCase));

                        if (keywordAttrInt != null || enumAttrInt != null)
                        {
                            string raw = ParseAttributeArg(keywordAttrInt ?? enumAttrInt) ?? "";
                            var entries = SplitCsv(raw);
                            if (entries.Count == 0)
                                entries = new List<string>() { "Option0", "Option1" };

                            var popup = new PopupField<string>(entries, 0) { label = displayName };
                            int currentIndex = Mathf.Clamp(material.GetInteger(propName), 0, entries.Count - 1);
                            popup.SetValueWithoutNotify(entries[currentIndex]);

                            popup.RegisterValueChangedCallback(evt =>
                            {
                                int selected = entries.IndexOf(evt.newValue);
                                material.SetInteger(propName, selected);
                                TryToggleKeywordSet(material, propName, entries, selected);
                                EditorUtility.SetDirty(material);
                            });

                            styleChild(popup);
                            currentContainer.Add(popup);
                            break;
                        }

                        // IntRange / RangeInt attribute handling -> use a Slider (rounded to int) and show input field if possible
                        var intRangeAttr = attributes.FirstOrDefault(a => a.IndexOf("IntRange", StringComparison.OrdinalIgnoreCase) >= 0
                                                                          || a.IndexOf("RangeInt", StringComparison.OrdinalIgnoreCase) >= 0
                                                                          || a.IndexOf("IntRange(", StringComparison.OrdinalIgnoreCase) >= 0
                                                                          || a.IndexOf("RangeInt(", StringComparison.OrdinalIgnoreCase) >= 0);
                        if (intRangeAttr != null)
                        {
                            string arg = ParseAttributeArg(intRangeAttr);
                            int min = 0, max = 1;
                            if (!string.IsNullOrEmpty(arg))
                            {
                                var parts = arg.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2)
                                {
                                    int.TryParse(parts[0].Trim(), out min);
                                    int.TryParse(parts[1].Trim(), out max);
                                }
                            }

                            // use float Slider but round values to int when writing back
                            var intSlider = new Slider(displayName, min, max) { value = material.GetInteger(propName) };
                            intSlider.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                            #region showInputField_safe_int
                            try
                            {
                                intSlider.showInputField = true;
                            }
                            catch { /* ignore if not available */ }
                            #endregion
                            intSlider.RegisterValueChangedCallback(evt =>
                            {
                                int v = Mathf.Clamp(Mathf.RoundToInt(evt.newValue), min, max);
                                material.SetInteger(propName, v);
                                EditorUtility.SetDirty(material);
                                // keep slider visually consistent (snapping)
                                intSlider.SetValueWithoutNotify(v);
                            });
                            styleChild(intSlider);
                            currentContainer.Add(intSlider);
                        }
                        else
                        {
                            Debug.Log($"The {propName} field value is {material.GetInteger(propName)}");
                            var intField = new IntegerField(displayName)
                            {
                                value = material.GetInteger(propName)
                            };
                            intField.RegisterValueChangedCallback(evt =>
                            {
                                material.SetInteger(propName, evt.newValue);
                                EditorUtility.SetDirty(material);
                            });
                            styleChild(intField);
                            currentContainer.Add(intField);
                        }
                    }
                        break;

                    default:
                    {
                        var label = new Label($"{displayName} ({propType})");
                        styleChild(label);
                        currentContainer.Add(label);
                    }
                        break;
                } // switch
            } // for
        } // Execute

        private static string ParseAttributeArg(string attr)
        {
            if (string.IsNullOrEmpty(attr)) return null;
            int start = attr.IndexOf('(');
            int end = attr.LastIndexOf(')');
            if (start >= 0 && end > start)
                return attr.Substring(start + 1, end - start - 1).Trim().Trim('"').Trim();
            return null;
        }

        private static List<string> SplitCsv(string s)
        {
            if (string.IsNullOrEmpty(s))
                return new List<string>();
            return s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
        }

        private static void TryToggleKeywordSet(Material mat, string propName, List<string> options, int selectedIndex)
        {
            var allCandidates = new List<string[]>();
            string cleanedProp = propName.StartsWith("_") ? propName.Substring(1) : propName;
            for (int i = 0; i < options.Count; ++i)
            {
                var opt = options[i].Replace(' ', '_').ToUpperInvariant();
                var pUpper = cleanedProp.ToUpperInvariant();
                var cands = new string[]
                {
                    $"_{pUpper}_{opt}",
                    $"{pUpper}_{opt}",
                    $"_{pUpper}{opt}",
                    $"{pUpper}{opt}",
                    $"_{propName}_{options[i]}",
                    $"{propName}_{options[i]}"
                };
                allCandidates.Add(cands);
            }

            foreach (var candArr in allCandidates)
            foreach (var k in candArr)
                mat.DisableKeyword(k);

            if (selectedIndex >= 0 && selectedIndex < allCandidates.Count)
            {
                foreach (var k in allCandidates[selectedIndex])
                {
                    try { mat.EnableKeyword(k); } catch { }
                }
            }
        }
    }
}
