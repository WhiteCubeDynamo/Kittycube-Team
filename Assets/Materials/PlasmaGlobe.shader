
    Shader "nimitz/PlasmaGlobe"
	{
	Properties{
	_MainTex("MainTex",2D) = "white"{} 

	}
	SubShader
	{
	Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
	Pass
	{
	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha
	CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
	#include "UnityCG.cginc"
			
    

    float4 vec4(float x,float y,float z,float w){return float4(x,y,z,w);}
    float4 vec4(float x){return float4(x,x,x,x);}
    float4 vec4(float2 x,float2 y){return float4(float2(x.x,x.y),float2(y.x,y.y));}
    float4 vec4(float3 x,float y){return float4(float3(x.x,x.y,x.z),y);}


    float3 vec3(float x,float y,float z){return float3(x,y,z);}
    float3 vec3(float x){return float3(x,x,x);}
    float3 vec3(float2 x,float y){return float3(float2(x.x,x.y),y);}

    float2 vec2(float x,float y){return float2(x,y);}
    float2 vec2(float x){return float2(x,x);}

    float vec(float x){return float(x);}
    
    

	struct VertexInput {
    float4 vertex : POSITION;
	float2 uv:TEXCOORD0;
    float4 tangent : TANGENT;
    float3 normal : NORMAL;
	//VertexInput
	};
	struct VertexOutput {
	float4 pos : SV_POSITION;
	float2 uv:TEXCOORD0;
	//VertexOutput
	};
	sampler2D _MainTex; 

	
	VertexOutput vert (VertexInput v)
	{
	VertexOutput o;
	o.pos = UnityObjectToClipPos (v.vertex);
	o.uv = v.uv;
	//VertexFactory
	return o;
	}
    
    // Plasma Globe by nimitz (twitter: @stormoid)
// https://www.shadertoy.com/view/XsjXRm
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License
// Contact the author for other licensing options

//looks best with around 25 rays
#define NUM_RAYS 3.

#define VOLUMETRIC_STEPS 1

#define MAX_ITER 3
#define FAR 1.

#define time _Time.y*1.1


float2x2 mm2(in float a){float c = cos(a), s = sin(a);return float2x2(c,-s,s,c);}
float noise( in float x ){return tex2Dlod(_MainTex,float4( vec2(x*.01,1.),0.0,0)).x;}

float hash( float n ){return frac(sin(n)*43758.5453);}

float noise(in float3 p)
{
	float3 ip = floor(p);
    float3 fp = frac(p);
	fp = fp*fp*(3.0-2.0*fp);
	
	float2 tap = (ip.xy+vec2(37.0,17.0)*ip.z) + fp.xy;
	float2 rg = tex2Dlod( _MainTex,float4( (tap + 0.5)/256.0, 0.0 ,0)).yx;
	return lerp(rg.x, rg.y, fp.z);
}

float3x3 m3 = float3x3( 0.00,  0.80,  0.60,
              -0.80,  0.36, -0.48,
              -0.60, -0.48,  0.64 );


//See: https://www.shadertoy.com/view/XdfXRj
float flow(in float3 p, in float t)
{
	float z=2.;
	float rz = 0.;
	float3 bp = p;
	for (float i= 1.;i < 5.;i++ )
	{
		p += time*.1;
		rz+= (sin(noise(p+t*0.8)*6.)*0.5+0.5) /z;
		p = lerp(bp,p,0.6);
		z *= 2.;
		p *= 2.01;
        p*= m3;
	}
	return rz;	
}

//could be improved
float sins(in float x)
{
 	float rz = 0.;
    float z = 2.;
    for (float i= 0.;i < 3.;i++ )
	{
        rz += abs(frac(x*1.4)-0.5)/z;
        x *= 1.3;
        z *= 1.15;
        x -= time*.65*z;
    }
    return rz;
}

float segm( float3 p, float3 a, float3 b)
{
    float3 pa = p - a;
	float3 ba = b - a;
	float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1. );	
	return length( pa - ba*h )*.5;
}

float3 path(in float i, in float d)
{
    float3 en = vec3(0.,0.,1.);
    float sns2 = sins(d+i*0.5)*0.22;
    float sns = sins(d+i*.6)*0.21;
    en.xz = mul(mm2((hash(i*10.569)-.5)*6.2+sns2), en.xz);
    en.xy = mul(mm2((hash(i*4.732)-.5)*6.2+sns), en.xy);
    return en;
}

float2 map(float3 p, float i)
{
	float lp = length(p);
    float3 bg = vec3(0.);   
    float3 en = path(i,lp);
    
    float ins = smoothstep(0.11,.46,lp);
    float outs = .15+smoothstep(.0,.15,abs(lp-1.));
    p *= ins*outs;
    float id = ins*outs;
    
    float rz = segm(p, bg, en)-0.011;
    return vec2(rz,id);
}

float march(in float3 ro, in float3 rd, in float startf, in float maxd, in float j)
{
	float precis = 0.001;
    float h=0.5;
    float d = startf;
    [unroll(100)]
for( int i=0; i<MAX_ITER; i++ )
    {
        if( abs(h)<precis||d>maxd ) break;
        d += h*1.2;
	    float res = map(ro+rd*d, j).x;
        h = res;
    }
	return d;
}

//volumetric marching
float3 vmarch(in float3 ro, in float3 rd, in float j, in float3 orig)
{   
    float3 p = ro;
    float2 r = vec2(0.);
    float3 sum = vec3(0);
    float w = 0.;
    [unroll(100)]
for( int i=0; i<VOLUMETRIC_STEPS; i++ )
    {
        r = map(p,j);
        p += rd*.03;
        float lp = length(p);
        
        float3 col = sin(vec3(1.05,2.5,1.52)*3.94+r.y)*.85+0.4;
        col.rgb *= smoothstep(.0,.015,-r.x);
        col *= smoothstep(0.04,.2,abs(lp-1.1));
        col *= smoothstep(0.1,.34,lp);
        sum += abs(col)*5. * (1.2-noise(lp*2.+j*13.+time*5.)*1.1) / (log(distance(p,orig)-2.)+.75);
    }
    return sum;
}

//returns both collision dists of unit sphere
float2 iSphere2(in float3 ro, in float3 rd)
{
    float3 oc = ro;
    float b = dot(oc, rd);
    float c = dot(oc,oc) - 1.;
    float h = b*b - c;
    if(h <0.0) return vec2(-1.);
    else return vec2((-b - sqrt(h)), (-b + sqrt(h)));
}


    
    
	fixed4 frag(VertexOutput vertex_output) : SV_Target
	{
		
	float2 p = vertex_output.uv/1-0.5;
	p.x*=1/1;
	float2 um = _iMouse.xy / 1-.5;
    
	//camera
	float3 ro = vec3(0.,0.,5.);
    float3 rd = normalize(vec3(p*.7,-1.5));
    float2x2 mx = mm2(time*.4+um.x*6.);
    float2x2 my = mm2(time*0.3+um.y*6.); 
    ro.xz = mul(mx, ro.xz);rd.xz = mul(mx, rd.xz);
    ro.xy = mul(my, ro.xy);rd.xy = mul(my, rd.xy);
    
    float3 bro = ro;
    float3 brd = rd;
	
    float3 col = vec3(0.0125,0.,0.025);
    #if 1
    for (float j = 1.;j<NUM_RAYS+1.;j++)
    {
        ro = bro;
        rd = brd;
        float2x2 mm = mm2((time*0.1+((j+1.)*5.1))*j*0.25);
        ro.xy = mul(mm, ro.xy);rd.xy = mul(mm, rd.xy);
        ro.xz = mul(mm, ro.xz);rd.xz = mul(mm, rd.xz);
        float rz = march(ro,rd,2.5,FAR,j);
		if ( rz >= FAR)continue;
    	float3 pos = ro+rz*rd;
    	col = max(col,vmarch(pos,rd,j, bro));
    }
    #endif
    
    ro = bro;
    rd = brd;
    float2 sph = iSphere2(ro,rd);
    
    if (sph.x > 0.)
    {
        float3 pos = ro+rd*sph.x;
        float3 pos2 = ro+rd*sph.y;
        float3 rf = reflect( rd, pos );
        float3 rf2 = reflect( rd, pos2 );
        float nz = (-log(abs(flow(rf*1.2,time)-.01)));
        float nz2 = (-log(abs(flow(rf2*1.2,-time)-.01)));
        col += (0.1*nz*nz* vec3(0.12,0.12,.5) + 0.05*nz2*nz2*vec3(0.55,0.2,.55))*0.8;
    }
    
	return vec4(col*1.3, 1.0);

	}
	ENDCG
	}
  }
  }
