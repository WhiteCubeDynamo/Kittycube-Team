Shader "Custom/URP/OptimizedSphere"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1
        _Radius ("Sphere Radius", Range(0.01, 2.0)) = 0.5
        _RimPower ("Rim Power", Range(0.1, 5.0)) = 2.0
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        [Toggle] _EnableRim ("Enable Rim Lighting", Float) = 0
        _SubsurfaceScattering ("Subsurface Scattering", Range(0, 2)) = 0.5
        _TransmissionColor ("Transmission Color", Color) = (0.8, 0.9, 1, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.5
        _SnowScale ("Snow Texture Scale", Range(0.0001, 100000.0)) = 4.0
        _SnowAmount ("Snow Texture Amount", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            // Unity keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Smoothness;
                float _Radius;
                float _RimPower;
                float4 _RimColor;
                float _EnableRim;
                float _SubsurfaceScattering;
                float4 _TransmissionColor;
                float _AmbientStrength;
                float _SnowScale;
                float _SnowAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 rayOriginOS : TEXCOORD1;
                float3 rayDirOS : TEXCOORD2;
                float3 centerWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Snow noise functions (adapted from Otavio Good's shader)
            float Hash3d(float3 uv)
            {
                float f = uv.x + uv.y * 37.0 + uv.z * 521.0;
                return frac(sin(f) * 110003.9);
            }
            
            float mixP(float f0, float f1, float a)
            {
                return lerp(f0, f1, a * a * (3.0 - 2.0 * a));
            }
            
            float noiseValue(float3 uv)
            {
                float3 fr = frac(uv.xyz);
                float3 fl = floor(uv.xyz);
                float3 zeroOne = float3(0.0, 1.0, 0.0);
                
                float h000 = Hash3d(fl);
                float h100 = Hash3d(fl + zeroOne.yxx);
                float h010 = Hash3d(fl + zeroOne.xyx);
                float h110 = Hash3d(fl + zeroOne.yyx);
                float h001 = Hash3d(fl + zeroOne.xxy);
                float h101 = Hash3d(fl + zeroOne.yxy);
                float h011 = Hash3d(fl + zeroOne.xyy);
                float h111 = Hash3d(fl + zeroOne.yyy);
                
                return mixP(
                    mixP(mixP(h000, h100, fr.x),
                         mixP(h010, h110, fr.x), fr.y),
                    mixP(mixP(h001, h101, fr.x),
                         mixP(h011, h111, fr.x), fr.y),
                    fr.z);
            }
            
            // Generate snow texture
            float GetSnowTexture(float3 pos)
            {
                float snowNoise = 0.0;
                float scale = _SnowScale;
                
                // Multi-octave noise for snow texture
                snowNoise += noiseValue(pos * scale) * 0.5;
                snowNoise += noiseValue(pos * scale * 2.0) * 0.25;
                snowNoise += noiseValue(pos * scale * 4.0) * 0.125;
                snowNoise += noiseValue(pos * scale * 8.0) * 0.0625;
                
                return snowNoise;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Transform to world space
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);

                // Calculate ray for sphere intersection in object space
                float3 cameraPositionOS = TransformWorldToObject(GetCameraPositionWS());
                output.rayOriginOS = cameraPositionOS;
                output.rayDirOS = normalize(input.positionOS.xyz - cameraPositionOS);

                // Store center position in world space
                output.centerWS = TransformObjectToWorld(float3(0, 0, 0));

                output.uv = input.uv;

                return output;
            }

            // Optimized sphere-ray intersection with proper center handling
            bool IntersectSphere(float3 rayOrigin, float3 rayDir, float radius, out float t, out float3 normal)
            {
                // Ray-sphere intersection with sphere at origin
                float a = dot(rayDir, rayDir);
                float b = 2.0 * dot(rayOrigin, rayDir);
                float c = dot(rayOrigin, rayOrigin) - radius * radius;
                
                float discriminant = b * b - 4.0 * a * c;
                
                if (discriminant < 0.0)
                {
                    t = -1.0;
                    normal = float3(0, 0, 0);
                    return false;
                }
                
                float sqrtDisc = sqrt(discriminant);
                float t1 = (-b - sqrtDisc) / (2.0 * a);
                float t2 = (-b + sqrtDisc) / (2.0 * a);
                
                // Choose the appropriate intersection
                if (t1 > 0.0)
                    t = t1;
                else if (t2 > 0.0)
                    t = t2;
                else
                {
                    t = -1.0;
                    normal = float3(0, 0, 0);
                    return false;
                }
                
                // Calculate hit point and normal
                float3 hitPoint = rayOrigin + rayDir * t;
                normal = normalize(hitPoint);
                
                return true;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float t;
                float3 normalOS;

                // Perform sphere intersection
                if (!IntersectSphere(input.rayOriginOS, input.rayDirOS, _Radius, t, normalOS))
                {
                    discard;
                }

                // Calculate world space position and normal
                float3 hitPointOS = input.rayOriginOS + input.rayDirOS * t;
                float3 hitPointWS = TransformObjectToWorld(hitPointOS);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);
                
                // Update depth buffer with correct depth
                float4 clipPos = TransformWorldToHClip(hitPointWS);
                
                #if defined(UNITY_REVERSED_Z)
                    // On platforms with reversed Z, depth goes from 1 (near) to 0 (far)
                    clipPos.z = max(0.0, clipPos.z);
                #else
                    // On platforms with normal Z, depth goes from -1 (near) to 1 (far)
                    clipPos.z = min(clipPos.w, clipPos.z);
                #endif
                
                // Write correct depth
                float depth = clipPos.z / clipPos.w;

                // Simple lighting with real directional light direction
                float3 viewDir = normalize(GetCameraPositionWS() - hitPointWS);
                
                // Get directional light direction (fallback to default if none)
                float3 lightDir = _MainLightPosition.xyz;
                if (length(lightDir) < 0.01)
                {
                    lightDir = normalize(float3(1, 1, 1)); // Fallback direction
                }
                
                // Basic Lambert lighting with controllable ambient
                float directionalStrength = 1.0 - _AmbientStrength;
                float NdotL = saturate(dot(normalWS, lightDir)) * directionalStrength + _AmbientStrength;
                
                // Specular lighting (Blinn-Phong)
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specularPower = _Smoothness * 128.0 + 1.0;
                float specular = pow(NdotH, specularPower) * _Smoothness;
                float3 specularColor = float3(1, 1, 1) * specular;
                
                // Subsurface scattering (light passing through snow)
                // Calculate how much light comes from behind relative to view direction
                float3 backLightDir = -lightDir;
                float backScatter = saturate(dot(viewDir, backLightDir));
                
                // Add some wrap-around scattering for more realistic effect
                float wrapScatter = saturate(dot(-normalWS, lightDir) + 0.5) * 0.5;
                
                // Combine both scattering effects
                float totalScatter = max(backScatter, wrapScatter);
                totalScatter = pow(totalScatter, 2.0);
                
                float3 subsurface = _TransmissionColor.rgb * totalScatter * _SubsurfaceScattering;
                
                // Optional rim lighting
                float rim = 0.0;
                if (_EnableRim > 0.5)
                {
                    rim = 1.0 - saturate(dot(viewDir, normalWS));
                    rim = pow(rim, _RimPower);
                }
                
                // Apply snow texture
                float3 worldPos = hitPointWS;
                float snowTexture = GetSnowTexture(worldPos);
                
                // Modulate base color with snow texture
                float3 texturedColor = lerp(_BaseColor.rgb, _BaseColor.rgb * (0.9 + snowTexture * 0.2), _SnowAmount);
                
                // Add subtle snow surface variation
                float snowVariation = snowTexture * _SnowAmount * 0.3;
                NdotL = saturate(NdotL + snowVariation * 0.1);
                
                // Boost overall brightness for snow
                float3 finalColor = texturedColor * NdotL + specularColor + subsurface + rim * _RimColor.rgb * _RimColor.a;
                finalColor *= 1.2; // Brighten everything
                
                float4 color = float4(finalColor, _BaseColor.a);
                
                // Apply fog
                float fogCoord = ComputeFogFactor(clipPos.z);
                color.rgb = MixFog(color.rgb, fogCoord);
                
                return color;
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Radius;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 rayOriginOS : TEXCOORD0;
                float3 rayDirOS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 cameraPositionOS = TransformWorldToObject(GetCameraPositionWS());
                output.rayOriginOS = cameraPositionOS;
                output.rayDirOS = normalize(input.positionOS.xyz - cameraPositionOS);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            bool IntersectSphereShadow(float3 rayOrigin, float3 rayDir, float radius, out float t)
            {
                float3 oc = rayOrigin;
                float a = dot(rayDir, rayDir);
                float b = 2.0 * dot(oc, rayDir);
                float c = dot(oc, oc) - radius * radius;

                float discriminant = b * b - 4 * a * c;

                if (discriminant < 0)
                {
                    t = 0;
                    return false;
                }

                float sqrt_discriminant = sqrt(discriminant);
                float t1 = (-b - sqrt_discriminant) / (2.0 * a);
                float t2 = (-b + sqrt_discriminant) / (2.0 * a);

                t = (t1 > 0) ? t1 : t2;
                return t > 0;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float t;
                if (!IntersectSphereShadow(input.rayOriginOS, input.rayDirOS, _Radius, t))
                {
                    discard;
                }

                return 0;
            }
            ENDHLSL
        }

        // Depth pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Radius;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 rayOriginOS : TEXCOORD0;
                float3 rayDirOS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 cameraPositionOS = TransformWorldToObject(GetCameraPositionWS());
                output.rayOriginOS = cameraPositionOS;
                output.rayDirOS = normalize(input.positionOS.xyz - cameraPositionOS);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            bool IntersectSphereDepth(float3 rayOrigin, float3 rayDir, float radius, out float t)
            {
                float3 oc = rayOrigin;
                float a = dot(rayDir, rayDir);
                float b = 2.0 * dot(oc, oc);
                float c = dot(oc, oc) - radius * radius;

                float discriminant = b * b - 4 * a * c;

                if (discriminant < 0)
                {
                    t = 0;
                    return false;
                }

                float sqrt_discriminant = sqrt(discriminant);
                float t1 = (-b - sqrt_discriminant) / (2.0 * a);
                float t2 = (-b + sqrt_discriminant) / (2.0 * a);

                t = (t1 > 0) ? t1 : t2;
                return t > 0;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float t;
                if (!IntersectSphereDepth(input.rayOriginOS, input.rayDirOS, _Radius, t))
                {
                    discard;
                }

                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
