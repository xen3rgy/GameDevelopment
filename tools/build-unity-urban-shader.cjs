// Keep HDRP's real lighting, shadows, depth and motion-vector passes. Only the
// surface/vertex hook is ours. Regenerate when the pinned HDRP version changes.
const fs = require('node:fs');
const path = require('node:path');
const pkg = process.argv[2] || 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.high-definition';
const target = path.resolve('unity/ZeroRise/Assets/ZeroRise/Art');
const source = fs.readFileSync(path.join(pkg, 'Runtime/Material/Lit/Lit.shader'), 'utf8');
const second = source.indexOf('    SubShader', source.indexOf('    SubShader') + 1);
if (second < 0) throw new Error('Expected HDRP raster/ray-tracing subshaders');
let shader = source.slice(0, second) + '    FallBack "Hidden/HDRP/FallbackError"\n    CustomEditor "Rendering.HighDefinition.LitGUI"\n}\n';
shader = shader.replace('Shader "HDRP/Lit"', 'Shader "ZeroRise/UrbanLit"');
shader = shader.replace('    #pragma target 4.5', `    #pragma target 4.5
    #pragma shader_feature_local _ _ZR_ROAD _ZR_FOLIAGE _ZR_TREE _ZR_SHRUB
    #if defined(_ZR_FOLIAGE) || defined(_ZR_TREE) || defined(_ZR_SHRUB)
    #define HAVE_MESH_MODIFICATION
    #define ATTRIBUTES_NEED_NORMAL
    #define ATTRIBUTES_NEED_TANGENT
    #define ATTRIBUTES_NEED_COLOR
    #endif
    #if defined(_ZR_FOLIAGE)
    #define ATTRIBUTES_NEED_TEXCOORD2
    #define ATTRIBUTES_NEED_TEXCOORD3
    #endif`);
shader = shader.replaceAll('"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"', '"UrbanLitData.hlsl"');
fs.writeFileSync(path.join(target, 'UrbanLit.shader'), '// Derived from Unity HDRP 17.6 Lit.shader; see HDRP-LICENSE.md.\n' + shader);
fs.copyFileSync(path.join(pkg, 'LICENSE.md'), path.join(target, 'HDRP-LICENSE.md'));
console.log('Generated UrbanLit raster passes from installed HDRP.');
