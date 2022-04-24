Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float3 BoundsMin;
            float3 BoundsMax;
            Texture3D<float4> Shape;
            SamplerState samplerShape;
            float smokeScale;

            float numSteps;
            float numStepsLight;

            float smokeLightAbsorb;

            float4 lPos;
            float4 lColor;
            float lIntensity;
            float lRadius;

            Texture3D<float4> EHash;
            Texture3D<float4> EHashC;
            float4 binsPerAxis;
            float4 gridSize;
            float4 gridMin;
            float binLength;

            float4 sunDir;
            float4 sunColor;
            float sunIntensity;

            float4 smokeColor;

            struct pointLight {
                float3 pos;
                float4 color;
            };

            float2 rayBoxIntersect(float3 bMin, float3 bMax, float3 ro, float3 rd) {
                //calculate intersects for each axis-alligned slab
                float3 t0 = (bMin - ro) / rd;
                float3 t1 = (bMax - ro) / rd;
                //order intersects
                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);
                //find intersection of all ranges
                float tEnter = max(tMin.x, max(tMin.y, tMin.z));
                float tExit = min(tMax.x, min(tMax.y, tMax.z));


                float distToBox = max(0, tEnter);
                float distThroughBox = max(0, tExit - distToBox);
                return float2(distToBox, distThroughBox);
            }

            float sampleDensity(float3 p) {
                //TODO replace with actuall function
                //float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                //return min(1, abs(noise.x + noise.y) * 0.5);

                //p = p * smokeScale * 0.01;
                //float4 samp = Shape.SampleLevel(samplerShape, p,0);
                //return samp.x;

                //uint3 pos = uint3(uint(floor(p.x * 30)) % 256, uint(floor(p.y * 30)) % 256, uint(floor(p.z * 30)) % 256);
                //float4 samp = Shape[pos];
                //float density = max(0, samp.r);
                //return density;

                return max(0, sin(p.x) + sin(p.y * 0.5) + sin(p.z * 0.1))/3.0;
            }

            int3 binCoord(float3 pos) {
                float3 relativePos = pos - gridMin.xyz;
                int3 posI = int3(floor(pos / binLength));
                return posI;
            }

            float3 colorToCoord(float4 c) {
                float3 t = float3(c.x * gridSize.x, c.y * gridSize.y, c.z * gridSize.z);
                return (t + gridMin.xyz);
            }

            pointLight ClosestLight(float3 pos) {
                pointLight ret;
                int3 b = binCoord(pos);
                float4 closest = float4(-1, -1, -1, -1);
                for (int i = b.x - 2; i <= b.x + 2; i++) {
                    for (int j = b.y - 2; j <= b.y + 2; j++) {
                        for (int k = b.z - 2; k <= b.z + 2; k++) {
                            int3 p = int3(i, j, k);
                            if (p.x < 0 || p.y < 0 || p.z < 0 || p.x > gridSize.x || p.y > gridSize.y || p.z > gridSize.z) continue;
                            if (EHash[p].w == 0) continue;
                            if (closest.w < 0 || distance(colorToCoord(EHash[p]), pos) < distance(closest.xyz, pos)) {
                                closest = float4(colorToCoord(EHash[p]),0);
                                ret.color = EHashC[p];
                            }
                        }
                    }
                }
                ret.pos = closest.xyz;
                return ret;
            }

            float3 zeroLight(float3 ro, float3 rd, float stepSize) {
                pointLight light = ClosestLight(ro);
                float3 lPos = light.pos;
                float3 rl = normalize(lPos - ro);
                float distToLight = length(lPos - ro);
                float rcos = max(0, dot(rd, rl)) * distToLight;
                //if (rcos < stepSize) return float3(1, 1, 1);
                //return float3(0, 0, 0);
                if (rcos > stepSize) return float3(0, 0, 0);
                
                float r2 = distToLight * distToLight;
                float distToCenter2 = r2 - (rcos * rcos);
                if (distToCenter2 > lRadius * lRadius) return float3(0, 0, 0);
                //float bri = -r2 + (rcos * rcos) + (lRadius*lRadius);
                //if (bri > 0) return float3(1, 1, 1);
                //bri = max(0, bri);
                float bri = lRadius * lRadius / (distToCenter2);
                bri = bri - 1;
                bri = min(bri, 10);
                return light.color *bri* lIntensity;
                //TODO: make render stop here so point lights are opaque?
            }

            //return estimate of how much light reaches a given point
            float3 ejectaMarch(float3 ro) {
                pointLight light = ClosestLight(ro);
                float3 lPos = light.pos;
                if (lPos.x < 0) return float3(0, 0, 0);
                //return float3(0, 1, 1);
                float3 rd = normalize(lPos - ro);
                float distToLight = length(lPos - ro);
                float2 smokeBoxI = rayBoxIntersect(BoundsMin, BoundsMax, ro, rd);
                float maxD = min(smokeBoxI.y, distToLight);
                float stepSize = smokeBoxI.y / numStepsLight;

                float t = 0;
                float totalDensity = 0;
                while (t < maxD) {
                    float3 p = ro + (rd * t);
                    totalDensity += sampleDensity(p);
                    t += stepSize;
                }
                float transmit = exp(-stepSize * totalDensity);
                return (light.color * transmit * lIntensity / (distToLight * distToLight));
            }

            float3 sunMarch(float3 ro) {
                float maxD = rayBoxIntersect(BoundsMin, BoundsMax, ro, sunDir).y;
                float stepSize = maxD / numStepsLight;
                float t = 0;
                float totalDensity = 0;
                while (t < maxD) {
                    float3 p = ro + (sunDir * t);
                    totalDensity += sampleDensity(p);
                    t += stepSize;
                }
                float transmit = exp(-stepSize * totalDensity);
                return sunColor.xyz * transmit * sunIntensity;
            }

            float3 lightMarch(float3 ro) {
                return ejectaMarch(ro) + sunMarch(ro);
            }

            //returns color of smoke
            float4 rayMarch(float3 ro, float3 rd, float maxD, float stepSize, float4 b) {

                float t = 0;
                float transmit = 1;
                float3 smokeDiffuse = 0;
                while (t < maxD) {
                    float3 p = ro + (rd * t);
                    float3 pointLight = lightMarch(p);
                    float pointDensity = sampleDensity(p);
                    transmit *= exp(-stepSize * pointDensity * smokeLightAbsorb);
                    smokeDiffuse += pointDensity * pointLight * transmit * stepSize;
                    smokeDiffuse += zeroLight(p, rd, stepSize) * transmit;
                    
                    if (transmit < 0.01) break;
                    t += stepSize;
                }
                return (b * transmit) + (float4(smokeDiffuse, 0.0) * smokeColor);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                float3 ro = _WorldSpaceCameraPos;
                float3 localD = mul(unity_CameraInvProjection, float4(i.uv * 2 - 1, 0, -1));
                float3 worldD = mul(unity_CameraToWorld, float4(localD, 0));
                float3 rd = normalize(worldD);

                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float depth = LinearEyeDepth(rawDepth) * length(worldD);

                float2 rayBIntersect = rayBoxIntersect(BoundsMin, BoundsMax, ro, rd);
                bool hit = ((rayBIntersect.y > 0.0) && (rayBIntersect.x < depth));
                //bool hit = (depth > 20);
                if (hit) {
                    col = rayMarch(ro + (rd * rayBIntersect.x), rd, min(depth - rayBIntersect.x, rayBIntersect.y), rayBIntersect.y / numSteps, col);
                }
                return col;
            }
            ENDCG
        }
    }
}
