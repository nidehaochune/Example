using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    public class PBRShader : BaseShaderGUI
    {
        static readonly string[] workflowModeNames = Enum.GetNames(typeof(PBRLitGUI.WorkflowMode));

        private PBRLitGUI.LitProperties litProperties;
        private PBRDetailGUI.LitProperties litDetailProperties;

        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(PBRDetailGUI.Styles.detailInputs, Expandable.Details, _ => PBRDetailGUI.DoDetailArea(litDetailProperties, materialEditor));
        }

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new PBRLitGUI.LitProperties(properties);
            litDetailProperties = new PBRDetailGUI.LitProperties(properties);
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, PBRLitGUI.SetMaterialKeywords, PBRDetailGUI.SetMaterialKeywords);
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            if (litProperties.workflowMode != null)
                DoPopup(PBRLitGUI.Styles.workflowModeText, litProperties.workflowMode, workflowModeNames);

            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            PBRLitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        // material main advanced options
        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                materialEditor.ShaderProperty(litProperties.highlights, PBRLitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, PBRLitGUI.Styles.reflectionsText);
            }

            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Blend", (float)blendMode);

            material.SetFloat("_Surface", (float)surfaceType);
            if (surfaceType == SurfaceType.Opaque)
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }


            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", (float)PBRLitGUI.WorkflowMode.Specular);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
            else
            {
                material.SetFloat("_WorkflowMode", (float)PBRLitGUI.WorkflowMode.Metallic);
                Texture texture = material.GetTexture("_MetallicGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
        }
    }
}
