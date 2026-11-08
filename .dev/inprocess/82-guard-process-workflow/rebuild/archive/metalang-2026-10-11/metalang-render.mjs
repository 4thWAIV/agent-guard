// Bundles metalang-app.jsx with the model inlined, into one self-contained metalang.html.
// It decides nothing about the page; the components do. This only moves bytes.
import * as yaml from 'js-yaml'
import { readFileSync, writeFileSync, unlinkSync } from 'node:fs'
import { build } from 'esbuild'

const model = yaml.load(readFileSync('metalang.yaml', 'utf8'))
// The example language travels as its own SOURCE TEXT, because the point of showing it is
// what a person types, not what it parses to.
const exampleText = readFileSync('example-id.yaml', 'utf8')
const instanceText = readFileSync('example-instance.yaml', 'utf8')
writeFileSync('.metalang-data.mjs',
  'export const MODEL = ' + JSON.stringify(model) + '\n' +
  'export const EXAMPLE = ' + JSON.stringify(exampleText) + '\n' +
  'export const INSTANCE = ' + JSON.stringify(instanceText) + '\n')

const { outputFiles } = await build({
  entryPoints: ['metalang-app.jsx'],
  bundle: true, write: false, format: 'iife', minify: true,
  jsx: 'automatic', target: 'es2020', logLevel: 'warning',
})
const script = outputFiles[0].text
unlinkSync('.metalang-data.mjs')

const css = `
  /* The palette is the one the earlier page used: warm paper, always light. */
  :root{
    --paper:#fbfaf7; --card:#fff; --ink:#1c2230; --soft:#4b5363; --dim:#8a909c;
    --rule:#e4e1d9; --band:#f4f2ed; --accent:#1f5f5b; --accent-soft:#e6f0ee;
    --bad:#a12b1f; --bad-soft:#fdeceb; --bad-line:#e9a79f;
    --ent:#1f5f5b; --fld:#3f5a7a; --lnk:#8a5212; --sca:#5a4a86; --cho:#8f3461; --opt:#6b7280;
    --abs:#8a909c;
    --sans:-apple-system,BlinkMacSystemFont,"Segoe UI",Inter,Helvetica,Arial,sans-serif;
    --mono:ui-monospace,SFMono-Regular,"SF Mono",Menlo,monospace;
  }
  *{box-sizing:border-box}
  body{margin:0;background:var(--paper);color:var(--ink);
       font:400 14px/1.6 var(--sans);-webkit-font-smoothing:antialiased}
  main{max-width:1180px;margin:0 auto;padding:34px 26px 90px}
  code{font-family:var(--mono)}

  .top{margin-bottom:24px}
  .eyebrow{font:700 10px/1 var(--sans);letter-spacing:.16em;text-transform:uppercase;
           color:var(--dim);margin:0 0 8px}
  h1{font:600 30px/1.2 var(--sans);margin:0 0 10px}
  .sub{font-size:14.5px;line-height:1.65;color:var(--soft);max-width:70ch;margin:0}
  .count{margin-top:14px;font:400 12px/1 var(--sans);color:var(--dim);
         font-variant-numeric:tabular-nums}

  nav{display:flex;flex-wrap:wrap;gap:6px;margin:20px 0 6px;
      padding-top:18px;border-top:1px solid var(--rule)}
  nav a{text-decoration:none}

  .grouphead{display:flex;align-items:baseline;gap:12px;margin:34px 0 2px}
  .grouphead h3{font:600 13px/1 var(--sans);letter-spacing:.11em;text-transform:uppercase;
                color:var(--ink);margin:0}
  .grouphead span{font-size:12.5px;color:var(--dim)}

  /* An entity is a box with a field list — the data-model shape. */
  .def{background:var(--card);border:1px solid var(--rule);border-radius:14px;
       padding:20px 22px;margin:12px 0}
  .def.is-entity{border-left:4px solid var(--ent)}
  .def.is-value{border-left:4px solid var(--sca)}
  .def h2{font:600 17px/1.3 var(--sans);margin:0 0 7px;
          display:flex;flex-wrap:wrap;align-items:center;gap:9px}
  .dname{font-family:var(--mono);font-size:17px;font-weight:600}
  .ext{font:400 12.5px/1 var(--sans);color:var(--dim)}
  .ext .ref{font-size:12.5px}
  p.title{margin:0 0 5px;font-size:14.5px}
  p.desc{margin:0;color:var(--soft);font-size:13.5px;max-width:76ch}
  p.of{margin:9px 0 0;font-size:13px;color:var(--soft)}
  p.of code{background:var(--band);border:1px solid var(--rule);border-radius:4px;
            padding:1px 6px;font-size:12px}

  .rowshead{margin:16px 0 0;font:700 9.5px/1 var(--sans);letter-spacing:.14em;
            text-transform:uppercase;color:var(--dim);display:flex;gap:10px;align-items:baseline}
  .rowcount{font:400 10px/1 var(--mono);letter-spacing:0;text-transform:none;color:var(--dim)}
  tr.is-inherited{opacity:.58}
  tr.is-inherited .c-name code{font-weight:400}
  .c-from{width:88px;text-align:right;white-space:nowrap;padding-right:0}
  .fromtag{font:500 10px/1 var(--mono);color:var(--soft);background:var(--band);
           border:1px solid var(--rule);border-radius:4px;padding:3px 6px;text-decoration:none}
  .fromtag:hover{color:var(--accent)}

  /* A link row is visibly a relationship; a field row is visibly a value. */
  tr.is-link{background:#fdf7f1}
  tr.is-link td:first-child{box-shadow:inset 3px 0 0 var(--lnk)}
  tr.is-field td:first-child{box-shadow:inset 3px 0 0 var(--fld)}
  tr.is-option td:first-child{box-shadow:inset 3px 0 0 var(--opt)}

  .c-mark{width:26px;padding-left:9px!important;padding-right:0}
  .mark{display:inline-block;width:16px;text-align:center;font-size:12px;line-height:1}
  .m-field{color:var(--fld);font-size:9px;position:relative;top:-1px}
  .m-link{color:var(--lnk);font-weight:700}
  .m-owns{font-size:11px}
  .m-option{color:var(--opt);font-size:8px;position:relative;top:-2px}

  .c-name{white-space:nowrap;padding-right:14px}
  .c-name code{font-size:13.5px;font-weight:600;color:var(--ink)}
  .pk{font:700 8.5px/1 var(--sans);letter-spacing:.09em;text-transform:uppercase;
      color:var(--accent);background:var(--accent-soft);border:1px solid #a9cdc8;
      border-radius:3px;padding:3px 4px;margin-left:7px;vertical-align:1px}

  .c-card{width:54px;white-space:nowrap;padding-right:14px}
  .card{display:inline-flex;align-items:center}
  .card>i{font:500 11px/1 var(--mono);font-style:normal;color:var(--dim);
          font-variant-numeric:tabular-nums}
  .foot{flex:none;overflow:visible}
  .foot .stem{stroke:#cfcbc0;stroke-width:1.4}
  .foot line:not(.stem){stroke:var(--accent);stroke-width:2;stroke-linecap:round}
  .foot circle{fill:var(--card);stroke:var(--accent);stroke-width:1.7}
  .foot .prongs{stroke:var(--accent);stroke-width:1.8;fill:none;stroke-linecap:round}

  .c-coll{width:46px;white-space:nowrap;padding-right:12px}
  .collwrap{display:inline-flex;align-items:center}
  .coll{flex:none;overflow:visible}
  .coll .rail{stroke:#b9b3a4;stroke-width:1.3;fill:none;stroke-linecap:round;stroke-linejoin:round}
  .coll .brace{stroke:#b9b3a4;stroke-width:1.4;fill:none;stroke-linecap:round}
  .coll .pips{fill:var(--accent)}
  .c-list .pips,.c-bag .pips{fill:#9a7a2a}
  .keyrow>span>em{font-style:normal;color:var(--dim);font-size:11px;margin-left:-2px}

  /* The type token is coloured by WHAT IT IS, so no reading tells scalar from entity. */
  .c-type{white-space:nowrap;width:1%;padding-right:14px}
  .tok{display:inline-block;font:600 12px/1 var(--mono);text-decoration:none;
       padding:4px 8px;border-radius:5px;border:1px solid}
  .t-scalar{color:#4a3a73;background:#f0ecf9;border-color:#cfc3ea}
  .t-choice{color:#7d2a53;background:#fbeaf2;border-color:#edc2d7}
  .t-entity{color:#1b544f;background:#e4f0ee;border-color:#a9cdc8}
  .t-abstract{color:#5c6270;background:#eef0f3;border-color:#d2d6dd;border-style:dashed}
  .tok:hover{filter:brightness(.96)}

  .c-tags{padding-right:14px}
  .c-desc{color:var(--soft);width:100%;font-size:13px}
  .pdetail{margin-top:3px;font-size:12px;color:var(--dim);max-width:62ch}
  .none{color:#d8d4c9}

  .key{margin:30px 0 0;padding:12px 16px;background:var(--card);
       border:1px solid var(--rule);border-radius:10px;display:grid;gap:7px}
  .keyrow{display:flex;flex-wrap:wrap;align-items:center;gap:6px 18px}
  .keyrow>i{font:700 9px/1.6 var(--sans);font-style:normal;letter-spacing:.13em;
            text-transform:uppercase;color:var(--dim);width:66px;flex:none}
  .keyrow>span{display:inline-flex;align-items:center;gap:7px;font-size:12px;color:var(--soft)}

  .ref{font-family:var(--mono);font-size:12.5px;color:var(--accent);
       text-decoration:none;border-bottom:1px solid #b9d4d0}
  .ref:hover{border-bottom-color:var(--accent)}
  .comma{color:var(--dim);margin-right:4px}
  .dangling{font-family:var(--mono);font-size:12.5px;color:var(--bad);
            text-decoration:line-through}

  .inbound{margin-top:14px;padding-top:11px;border-top:1px dashed var(--rule);
           display:flex;flex-wrap:wrap;align-items:baseline;gap:8px}
  .inbound>i{font:700 9px/1.6 var(--sans);font-style:normal;letter-spacing:.13em;
             text-transform:uppercase;color:var(--dim)}
  .inref{font-family:var(--mono);font-size:11.5px;background:var(--band);
         border:1px solid var(--rule);border-radius:5px;padding:2px 7px}
  .inref a{color:var(--ink);text-decoration:none;font-weight:500}
  .inref a:hover{color:var(--accent)}
  .inref em{font-style:normal;color:var(--dim)}

  .kind{font:650 10px/1 var(--sans);letter-spacing:.07em;text-transform:uppercase;
        padding:3px 7px;border-radius:999px;border:1px solid currentColor;white-space:nowrap;
        display:inline-block}
  .k-entity{color:var(--ent)}.k-field{color:var(--fld)}.k-link{color:var(--lnk)}
  .k-scalar{color:var(--sca)}.k-choice{color:var(--cho)}.k-option{color:var(--opt)}
  .k-part,.k-requirement,.k-abstract{color:var(--abs)}
  .k-document{color:var(--soft)}

  .tags{margin-top:5px;display:flex;flex-wrap:wrap;gap:5px}
  .tags{margin-top:0;display:flex;flex-wrap:wrap;gap:4px}
  .coll{display:inline-flex;align-items:center;gap:5px;font:650 10.5px/1 var(--sans);
        letter-spacing:.05em;text-transform:uppercase;padding:3px 7px;border-radius:4px;
        border:1px solid}
  .coll b{font:700 11px/1 var(--mono);letter-spacing:-1px;opacity:.75}
  .c-roster{color:#1b544f;background:#e4f0ee;border-color:#a9cdc8}
  .c-list{color:#7a4a0d;background:#fbf0e3;border-color:#e3c69c}
  .c-set{color:#4a3a73;background:#f0ecf9;border-color:#cfc3ea}
  .c-bag{color:#6b7280;background:#eef0f3;border-color:#d2d6dd}
  /* A tag never breaks inside itself. That makes its min-content width the whole tag, so
     the table cannot squeeze the column down and stack the text one letter per line. */
  .tag{font:400 11px/1.5 var(--mono);padding:2px 6px;border-radius:4px;
       background:var(--band);border:1px solid var(--rule);color:var(--soft);
       white-space:nowrap}
  .tag b{font-weight:600;color:var(--ink)}
  .taglist{padding:0;position:relative}
  .taglist>summary{padding:2px 6px;cursor:pointer;list-style:none;white-space:nowrap}
  .taglist>summary::-webkit-details-marker{display:none}
  .taglist>summary::after{content:" \\25be";color:var(--dim)}
  .taglist[open]>summary::after{content:" \\25b4"}
  .taglist[open]{background:var(--card);border-color:#cfcbc0}
  .def table{position:relative}
  .taglist[open]>.vals{position:absolute;left:-1px;top:calc(100% + 4px);width:430px;
       z-index:5;background:var(--card);border:1px solid #cfcbc0;border-radius:7px;
       padding:8px;box-shadow:0 8px 22px rgba(28,34,48,.11)}
  .vals{display:flex;flex-wrap:wrap;gap:3px}
  .vals code{font-size:10.5px;background:var(--band);border:1px solid var(--rule);
             border-radius:3px;padding:1px 5px;color:var(--soft);white-space:nowrap}

  .diagram{margin:26px 0 8px;padding:20px 22px 16px;background:var(--card);
           border:1px solid var(--rule);border-radius:14px}
  .diagram svg{display:block;max-width:100%;height:auto}
  .diagram figcaption{margin-top:12px;padding-top:11px;border-top:1px solid var(--rule);
                      font-size:12px;line-height:1.6;color:var(--dim);max-width:78ch}
  .n rect{fill:var(--accent-soft);stroke:var(--accent);stroke-width:1.4}
  .n text{font:600 12.5px var(--mono);fill:var(--ink)}
  .n.is-abstract rect{fill:var(--band);stroke:var(--dim);stroke-dasharray:4 3}
  .n.is-abstract text{fill:var(--soft);font-style:italic}
  .n:hover rect{fill:#d8e8e5}
  .e-inherit{stroke:var(--soft);stroke-width:1.3}
  .e-contain path{stroke:var(--lnk);stroke-width:1.4}
  .e-contain text{font:500 10.5px var(--mono);fill:var(--lnk);
                  paint-order:stroke;stroke:var(--card);stroke-width:4px;stroke-linejoin:round}

  /* The neighbourhood. One definition, one hop out, both directions. */
  .hood{margin:14px 0 8px;padding:18px 20px 14px;background:var(--card);
        border:1px solid var(--rule);border-radius:14px}
  .hoodhead{display:flex;align-items:baseline;gap:12px;margin-bottom:10px;flex-wrap:wrap}
  .hoodhead h3{font:600 13px/1 var(--sans);letter-spacing:.09em;text-transform:uppercase;margin:0}
  .hoodhead code{font-family:var(--mono);text-transform:none;letter-spacing:0;color:var(--accent)}
  .hoodhead span{font-size:12px;color:var(--dim)}
  .hoodscroll{overflow-x:auto}
  .hood svg{display:block;max-width:100%;height:auto;margin:0 auto}
  .nn rect{fill:var(--band);stroke:#cfcbc0;stroke-width:1.2}
  .nn text{font:500 12px var(--mono);fill:var(--soft);text-anchor:middle}
  .nn{cursor:pointer}
  .nn:hover rect{fill:#eceae2;stroke:var(--soft)}
  .nn.is-focus rect{fill:var(--accent-soft);stroke:var(--accent);stroke-width:1.8}
  .nn.is-focus text{fill:var(--ink);font-weight:700}
  .nn.is-focus{cursor:default}
  .edge path{stroke:#c9ac8a;stroke-width:1.3;fill:none}
  .edge.is-owns path{stroke:var(--lnk);stroke-width:2.4}
  .edge text{font:500 10px var(--mono);fill:var(--lnk);opacity:.9;
             paint-order:stroke;stroke:var(--card);stroke-width:3.5px;stroke-linejoin:round}
  .edge text.end{text-anchor:end}
  .hood figcaption{margin-top:12px;padding-top:11px;border-top:1px solid var(--rule);
                   font-size:12px;line-height:1.6;color:var(--dim);max-width:80ch}
  .hood figcaption code{font-family:var(--mono);color:var(--accent)}

  /* The cheatsheet. What may be written inside each metaclass. */
  .sheet{margin:34px 0 0}
  .sheethead{display:flex;align-items:baseline;gap:12px;margin:0 0 10px}
  .sheethead h3{font:600 13px/1 var(--sans);letter-spacing:.11em;text-transform:uppercase;margin:0}
  .sheethead span{font-size:12.5px;color:var(--dim)}
  .sheetgrid{display:grid;grid-template-columns:repeat(auto-fill,minmax(330px,1fr));gap:12px}
  .sheetcard{background:var(--card);border:1px solid var(--rule);border-radius:12px;
             padding:14px 16px}
  .sheetcard h4{margin:0 0 9px;font:600 14px/1 var(--mono)}
  .sheetcard h4 a{color:var(--ink);text-decoration:none}
  .sheetcard h4 a:hover{color:var(--accent)}
  .sheetrow{display:flex;gap:10px;align-items:baseline;margin-top:7px}
  .sheetrow>i{font:700 9px/1.6 var(--sans);font-style:normal;letter-spacing:.13em;
              text-transform:uppercase;color:var(--dim);width:34px;flex:none}
  .sheetrow>span{display:flex;flex-wrap:wrap;gap:4px}
  .sheetrow code{font:500 11.5px/1.5 var(--mono);background:var(--band);
                 border:1px solid var(--rule);border-radius:4px;padding:2px 6px;color:var(--soft)}
  .sheetrow code.must{color:var(--accent);background:var(--accent-soft);border-color:#a9cdc8;
                      font-weight:650}

  /* A language written in MetaLang, as the text a person types. */
  .stack{display:grid;grid-template-columns:repeat(auto-fit,minmax(420px,1fr));gap:12px}
  .layer{display:flex;align-items:baseline;gap:9px;margin:0 0 7px}
  .layernum{font:700 9px/1 var(--sans);letter-spacing:.1em;color:#fff;background:var(--accent);
            border-radius:999px;padding:4px 8px}
  .layername{font:600 13px/1 var(--mono)}
  .layerwhat{font-size:12px;color:var(--dim)}
  .usedin{margin:34px 0 0}
  .usedinhead{display:flex;align-items:baseline;gap:12px;margin:0 0 10px;flex-wrap:wrap}
  .usedinhead h3{font:600 13px/1 var(--sans);letter-spacing:.11em;text-transform:uppercase;margin:0}
  .usedinhead span{font-size:12.5px;color:var(--dim)}
  .usedin pre{margin:0;padding:18px 20px;background:var(--card);border:1px solid var(--rule);
              border-radius:12px;overflow-x:auto;
              font:400 12.5px/1.75 var(--mono);color:var(--ink)}
  .usedin .cmt{color:var(--dim)}
  .usedin .k{color:var(--soft)}
  .usedin .str{color:#7a4a0d}
  .usedin .lnk{text-decoration:none;padding:1px 5px;border-radius:4px;border:1px solid;
               font-weight:600;font-size:11.5px}
  .usedin .verdict{margin-top:10px;display:inline-flex;align-items:center;gap:8px;
                   font-size:12.5px;color:var(--accent);background:var(--accent-soft);
                   border:1px solid #a9cdc8;border-radius:7px;padding:6px 11px}

  /* The error display. What you get when nothing describes the data. */
  .err{display:flex;align-items:baseline;gap:8px;flex-wrap:wrap;
       background:var(--bad-soft);border:1px solid var(--bad-line);border-radius:5px;
       padding:5px 9px;margin:8px 0;max-width:100%}
  .err>em{font:800 10px/1 var(--sans);font-style:normal;color:#fff;background:var(--bad);
          padding:4px 6px;border-radius:3px;flex:none}
  .err>b{font:600 12px/1.45 var(--sans);color:var(--bad)}
  .err>code{font-size:11px;color:#7a2017;background:#fff;border:1px solid var(--bad-line);
            border-radius:3px;padding:2px 5px}
  .err>pre{margin:3px 0 0;flex-basis:100%;font:400 11px/1.5 var(--mono);
           white-space:pre-wrap;color:#7a2017}

  @media (max-width:720px){
    main{padding:24px 16px 70px}
    .c-name,.c-type,.c-card{white-space:normal}
  }`

writeFileSync('metalang.html', `<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>${model.title}</title>
<style>${css}</style></head>
<body><div id="root"></div><script>${script}</script></body></html>`)

console.log(`metalang.html — React, ${(script.length / 1024).toFixed(0)} KB bundled, nothing fetched at load`)
