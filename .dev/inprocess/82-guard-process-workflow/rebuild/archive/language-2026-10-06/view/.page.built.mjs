// view/page.jsx
import React2 from "react";

// view/ui.jsx
import React from "react";

// view/model.mjs
import { readFileSync } from "node:fs";
import yaml from "js-yaml";
var at = (f) => new URL(`../${f}`, import.meta.url);
var load = (f) => yaml.load(readFileSync(at(f), "utf8"));
var language = load("language.yaml");
var architecture = load("architecture.yaml");
var contract = load("contract.yaml");
var index = (list3) => Object.fromEntries((list3 ?? []).map((x) => [x.id, x]));
var vocab = {
  form: index(language.forms),
  member: index(language.members),
  visibility: index(language.visibility),
  alteration: index(language.alteration),
  relation: index(language.relations),
  storage: index(language.storage),
  precision: index(language.precision),
  primitive: index(language.primitives),
  external: index(language.external),
  actor: index(language.actors),
  lens: index(language.lenses),
  research: index(language.research),
  gateKind: index(language.gate_kinds),
  gateCondition: index(language.gate_condition_kinds),
  constraintKind: index(language.constraint_kinds),
  permissionVerb: index(language.permission_verbs),
  prose: index(language.prose),
  level: index(language.levels),
  tone: index(language.tones),
  view: index(language.views),
  dimension: index(language.dimensions),
  device: index(language.device)
};
var payloadKinds = architecture.relations.filter((r) => r.kind === "implements" && r.to === "Payload").map((r) => r.from);

// view/ui.jsx
import { Fragment, jsx, jsxs } from "react/jsx-runtime";
var Draw = ({ node }) => {
  if (node == null) return /* @__PURE__ */ jsx(Undescribed, { node: { why: "nothing here", at: "", value: node } });
  if (Array.isArray(node)) return /* @__PURE__ */ jsx(Fragment, { children: node.map((n, i) => /* @__PURE__ */ jsx(Draw, { node: n }, i)) });
  const C = DEVICES[node.kind];
  if (!C) return /* @__PURE__ */ jsx(Undescribed, { node: { why: `no component draws a "${node.kind}"`, at: "", value: node } });
  return /* @__PURE__ */ jsx(C, { node });
};
var Each = ({ nodes }) => /* @__PURE__ */ jsx(Fragment, { children: (nodes ?? []).map((n, i) => /* @__PURE__ */ jsx(Draw, { node: n }, i)) });
var show = (v) => v == null ? "\u2014" : typeof v === "object" ? JSON.stringify(v).slice(0, 120) : String(v).slice(0, 120);
var Undescribed = ({ node }) => /* @__PURE__ */ jsxs("span", { className: "d-err", title: node.at || void 0, children: [
  /* @__PURE__ */ jsx("em", { children: "!" }),
  /* @__PURE__ */ jsx("b", { children: node.why }),
  /* @__PURE__ */ jsx("code", { children: show(node.value) })
] });
var Undesigned = ({ node }) => /* @__PURE__ */ jsxs("div", { className: "d-undesigned", children: [
  /* @__PURE__ */ jsx("span", { className: "d-undesigned-mark", children: "undesigned" }),
  /* @__PURE__ */ jsx("b", { children: node.name ?? "unnamed" }),
  /* @__PURE__ */ jsx("span", { children: "No members, no research, no questions, no gate." })
] });
var KindChip = ({ node }) => /* @__PURE__ */ jsx("em", { className: `d-chip k-${node.id}`, title: node.name, children: node.mark });
var Reach = ({ node }) => {
  const filled = { public: 3, internal: 2, private: 1 }[node.id] ?? 0;
  return /* @__PURE__ */ jsx("b", { className: "d-reach", title: node.name, children: [1, 2, 3].map((i) => /* @__PURE__ */ jsx("i", { className: i <= filled ? "on" : "" }, i)) });
};
var Alteration = ({ node }) => node.id === "unchanged" ? null : /* @__PURE__ */ jsx("em", { className: `d-alt a-${node.id}`, title: node.name, children: node.mark });
var Reference = ({ node }) => {
  if (node.gap) return /* @__PURE__ */ jsx(Draw, { node: node.gap });
  const badge = { defined: "\u25C7", primitive: "#", external: "\u2197" }[node.resolves];
  const body = /* @__PURE__ */ jsxs(Fragment, { children: [
    /* @__PURE__ */ jsx("em", { className: "d-ref-badge", children: badge }),
    /* @__PURE__ */ jsx("b", { children: node.name }),
    node.arguments.length > 0 && /* @__PURE__ */ jsxs(Fragment, { children: [
      /* @__PURE__ */ jsx("i", { children: "<" }),
      node.arguments.map((a, i) => /* @__PURE__ */ jsxs(React.Fragment, { children: [
        i > 0 && /* @__PURE__ */ jsx("i", { children: "," }),
        /* @__PURE__ */ jsx(Draw, { node: a })
      ] }, i)),
      /* @__PURE__ */ jsx("i", { children: ">" })
    ] })
  ] });
  return node.resolves === "defined" ? /* @__PURE__ */ jsx(
    "button",
    {
      type: "button",
      className: "d-ref r-defined d-open",
      "data-def": node.name,
      "aria-expanded": "false",
      title: "Defined here. Click to open.",
      children: body
    }
  ) : /* @__PURE__ */ jsx("span", { className: `d-ref r-${node.resolves}`, children: body });
};
var Refs = ({ label, nodes, note }) => !nodes?.length ? null : /* @__PURE__ */ jsxs("div", { className: "d-io", children: [
  /* @__PURE__ */ jsx("i", { children: label }),
  /* @__PURE__ */ jsx("span", { children: nodes.map((n, idx) => /* @__PURE__ */ jsxs(React.Fragment, { children: [
    idx > 0 && /* @__PURE__ */ jsx("i", { className: "d-or", children: "or" }),
    /* @__PURE__ */ jsx(Draw, { node: n })
  ] }, idx)) }),
  note && /* @__PURE__ */ jsx("em", { children: note })
] });
var Process = ({ node }) => {
  if (node.undesigned) return /* @__PURE__ */ jsx(Draw, { node: node.undesigned });
  const entry = node.gates.filter((g) => g.spec?.id === "entry");
  const exit = node.gates.filter((g) => g.spec?.id === "exit");
  const other = node.gates.filter((g) => g.kind === "undescribed");
  return /* @__PURE__ */ jsxs("div", { className: "d-process", children: [
    /* @__PURE__ */ jsxs("div", { className: "d-process-head", children: [
      /* @__PURE__ */ jsx(Draw, { node: { kind: "form", ...vocab.form.process } }),
      /* @__PURE__ */ jsx("b", { children: node.name ?? /* @__PURE__ */ jsx(Undescribed, { node: { why: "a process with no name", value: null } }) }),
      /* @__PURE__ */ jsxs("i", { className: "d-count", children: [
        node.members.length,
        " members"
      ] })
    ] }),
    /* @__PURE__ */ jsx(Refs, { label: "takes", nodes: node.takes }),
    /* @__PURE__ */ jsx(Each, { nodes: entry }),
    /* @__PURE__ */ jsx(Research, { nodes: node.research }),
    /* @__PURE__ */ jsx(Questions, { nodes: node.questions }),
    node.note && /* @__PURE__ */ jsx("p", { className: "d-note", children: node.note }),
    /* @__PURE__ */ jsx("div", { className: "d-members", children: node.members.map((mem, i) => /* @__PURE__ */ jsxs("div", { className: "d-member", children: [
      /* @__PURE__ */ jsx("i", { className: "d-ord", children: i + 1 }),
      /* @__PURE__ */ jsx("div", { children: /* @__PURE__ */ jsx(Draw, { node: mem }) })
    ] }, i)) }),
    /* @__PURE__ */ jsx(Each, { nodes: other }),
    /* @__PURE__ */ jsx(Each, { nodes: exit }),
    /* @__PURE__ */ jsx(Refs, { label: "produces", nodes: node.produces }),
    /* @__PURE__ */ jsx(Each, { nodes: node.extra })
  ] });
};
var Step = ({ node }) => /* @__PURE__ */ jsxs("div", { className: `d-step act-${node.actor.id}`, children: [
  /* @__PURE__ */ jsxs("div", { className: "d-step-top", children: [
    /* @__PURE__ */ jsx("b", { children: node.unnamed ? /* @__PURE__ */ jsx(Draw, { node: node.unnamed }) : node.name }),
    node.fansOut && /* @__PURE__ */ jsx("span", { className: "d-fanout", children: node.fansOut }),
    /* @__PURE__ */ jsx("em", { className: "d-actor", children: node.actor.name })
  ] }),
  /* @__PURE__ */ jsx(Refs, { label: "takes", nodes: node.takes }),
  /* @__PURE__ */ jsx(Refs, { label: "produces", nodes: node.produces }),
  node.note && /* @__PURE__ */ jsxs("div", { className: "d-io", children: [
    /* @__PURE__ */ jsx("i", {}),
    /* @__PURE__ */ jsx("em", { className: "d-stepnote", children: node.note })
  ] }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var Research = ({ nodes }) => !nodes?.length ? null : /* @__PURE__ */ jsxs("div", { className: "d-band b-research", children: [
  /* @__PURE__ */ jsx("span", { className: "d-band-lab", children: "Research" }),
  /* @__PURE__ */ jsx("span", { className: "d-band-body", children: /* @__PURE__ */ jsx(Each, { nodes }) })
] });
var ResearchNeed = ({ node }) => {
  const cls = `d-pill${node.required ? " p-required" : ""}`;
  const body = /* @__PURE__ */ jsxs(Fragment, { children: [
    /* @__PURE__ */ jsx("b", { children: node.spec.name }),
    /* @__PURE__ */ jsx("i", { children: node.required ? "required" : "optional" })
  ] });
  return node.hasProcess ? /* @__PURE__ */ jsxs(
    "button",
    {
      type: "button",
      className: `${cls} d-open`,
      "data-def": node.spec.name,
      "aria-expanded": "false",
      title: node.spec.description,
      children: [
        body,
        /* @__PURE__ */ jsx("em", { className: "d-pill-arrow", children: "\u203A" })
      ]
    }
  ) : /* @__PURE__ */ jsxs("span", { className: cls, title: node.spec.description, children: [
    body,
    /* @__PURE__ */ jsx("em", { className: "d-pill-none", children: "no steps" })
  ] });
};
var Questions = ({ nodes }) => !nodes?.length ? null : /* @__PURE__ */ jsxs("div", { className: "d-band b-questions", children: [
  /* @__PURE__ */ jsx("span", { className: "d-band-lab", children: "Questions" }),
  /* @__PURE__ */ jsx("span", { className: "d-band-body", children: /* @__PURE__ */ jsx(Each, { nodes }) })
] });
var Question = ({ node }) => /* @__PURE__ */ jsxs("div", { className: "d-question", children: [
  /* @__PURE__ */ jsx("span", { className: "d-question-text", children: node.text }),
  /* @__PURE__ */ jsx("span", { className: "d-question-by", children: node.answeredBy.map((r, i) => r.kind === "undescribed" ? /* @__PURE__ */ jsx(Draw, { node: r }, i) : /* @__PURE__ */ jsx("i", { children: r.name }, i)) }),
  node.unanswerable && /* @__PURE__ */ jsx(Draw, { node: node.unanswerable }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var Gate = ({ node }) => /* @__PURE__ */ jsxs("div", { className: `d-gate g-${node.spec.id}`, children: [
  /* @__PURE__ */ jsxs("div", { className: "d-gate-head", children: [
    /* @__PURE__ */ jsx("em", { className: "d-gate-kind", children: node.spec.name }),
    /* @__PURE__ */ jsx("span", { children: node.spec.description }),
    /* @__PURE__ */ jsxs("i", { className: "d-count", children: [
      node.conditions.length,
      " conditions"
    ] })
  ] }),
  node.empty && /* @__PURE__ */ jsx(Draw, { node: node.empty }),
  /* @__PURE__ */ jsx("div", { className: "d-gate-body", children: /* @__PURE__ */ jsx(Each, { nodes: node.conditions }) }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var GateCondition = ({ node }) => /* @__PURE__ */ jsxs("div", { className: "d-cond", children: [
  /* @__PURE__ */ jsx("span", { className: "d-cond-kind", children: node.spec.name }),
  /* @__PURE__ */ jsxs("span", { className: "d-cond-body", children: [
    /* @__PURE__ */ jsx("b", { children: node.over ?? /* @__PURE__ */ jsx(Undescribed, { node: { why: "a condition with nothing to be over", value: null } }) }),
    /* @__PURE__ */ jsx("em", { children: node.spec.question })
  ] }),
  node.by && /* @__PURE__ */ jsxs("span", { className: "d-cond-by", children: [
    /* @__PURE__ */ jsx("i", { children: "satisfied by" }),
    node.by
  ] }),
  /* @__PURE__ */ jsx(Each, { nodes: node.missing }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var Element = ({ node }) => /* @__PURE__ */ jsxs("div", { className: `d-element f-${node.form?.id ?? "unknown"}${node.generated ? " is-generated" : ""}`, children: [
  /* @__PURE__ */ jsxs("div", { className: "d-element-head", children: [
    node.form ? /* @__PURE__ */ jsx(Draw, { node: node.form }) : null,
    /* @__PURE__ */ jsx("i", { className: "d-form-name", children: node.form?.name.toLowerCase() }),
    /* @__PURE__ */ jsx("b", { children: node.name }),
    node.parameters.length > 0 && /* @__PURE__ */ jsxs("span", { className: "d-params", children: [
      "<",
      node.parameters.join(", "),
      ">"
    ] }),
    node.storage && /* @__PURE__ */ jsx("span", { className: "d-store", title: node.storage.name, children: node.storage.id }),
    node.precision && /* @__PURE__ */ jsx("span", { className: "d-prec", children: node.precision.id }),
    node.alteration && /* @__PURE__ */ jsx(Draw, { node: node.alteration }),
    node.generated && /* @__PURE__ */ jsx("span", { className: "d-gen", children: "generated" })
  ] }),
  /* @__PURE__ */ jsx(Each, { nodes: node.bad }),
  /* @__PURE__ */ jsx("div", { className: "d-element-body", children: /* @__PURE__ */ jsx(Each, { nodes: node.members }) }),
  node.purpose && /* @__PURE__ */ jsx("p", { className: "d-purpose", children: node.purpose }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var Member = ({ node }) => /* @__PURE__ */ jsxs("div", { className: "d-mrow", children: [
  node.visibility ? /* @__PURE__ */ jsx(Draw, { node: node.visibility }) : null,
  /* @__PURE__ */ jsx("span", { className: "d-mname", children: node.name }),
  node.memberKind ? /* @__PURE__ */ jsx("em", { className: `d-mkind m-${node.memberKind.id}`, title: node.memberKind.name, children: node.memberKind.mark }) : null,
  /* @__PURE__ */ jsx("span", { className: "d-msig", children: node.type ? /* @__PURE__ */ jsx(Draw, { node: node.type }) : node.note ? /* @__PURE__ */ jsx("em", { className: "d-mnote", children: node.note }) : null }),
  node.alteration && /* @__PURE__ */ jsx(Draw, { node: node.alteration }),
  /* @__PURE__ */ jsx(Each, { nodes: node.bad }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var Relation = ({ node }) => /* @__PURE__ */ jsxs("span", { className: "d-rel", children: [
  /* @__PURE__ */ jsx(Draw, { node: node.from }),
  /* @__PURE__ */ jsx("em", { className: "d-rel-kind", children: node.spec.name }),
  /* @__PURE__ */ jsx(Draw, { node: node.to }),
  node.toArguments.length > 0 && /* @__PURE__ */ jsx("span", { className: "d-rel-args", children: /* @__PURE__ */ jsx(Each, { nodes: node.toArguments }) }),
  node.note && /* @__PURE__ */ jsx("span", { className: "d-rel-note", children: node.note }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var RelationEnd = ({ node }) => /* @__PURE__ */ jsxs("span", { className: "d-rel-end", children: [
  /* @__PURE__ */ jsx("b", { children: node.element }),
  node.member && /* @__PURE__ */ jsxs("i", { children: [
    ".",
    node.member
  ] })
] });
var Constraint = ({ node }) => /* @__PURE__ */ jsxs("div", { className: `d-constraint c-${node.spec.id}`, children: [
  /* @__PURE__ */ jsxs("div", { className: "d-constraint-head", children: [
    /* @__PURE__ */ jsx("b", { children: node.spec.name }),
    /* @__PURE__ */ jsx("code", { children: node.id }),
    /* @__PURE__ */ jsx("i", { className: `d-enf e-${node.enforcement}`, children: node.enforcement })
  ] }),
  /* @__PURE__ */ jsx("div", { className: "d-constraint-body", children: Object.entries(node.fields).map(([k, v]) => /* @__PURE__ */ jsxs("span", { className: "d-field", children: [
    /* @__PURE__ */ jsx("i", { children: k }),
    /* @__PURE__ */ jsx("b", { children: String(v) })
  ] }, k)) }),
  node.source && /* @__PURE__ */ jsxs("p", { className: "d-source", children: [
    /* @__PURE__ */ jsx("i", { children: "source" }),
    node.source
  ] }),
  /* @__PURE__ */ jsx(Each, { nodes: node.missing }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var Payload = ({ node }) => /* @__PURE__ */ jsx(Draw, { node: node.body });
var PayloadElsewhere = ({ node }) => /* @__PURE__ */ jsxs("div", { className: "d-elsewhere", children: [
  /* @__PURE__ */ jsx("span", { children: "drawn in full in" }),
  /* @__PURE__ */ jsx("a", { href: node.in.replace(".yaml", ".html"), children: /* @__PURE__ */ jsx("code", { children: node.in }) }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var Sentence = ({ node }) => /* @__PURE__ */ jsxs("div", { className: "d-sentence", children: [
  /* @__PURE__ */ jsx("div", { className: "d-sentence-text", children: node.text }),
  /* @__PURE__ */ jsx(Draw, { node: node.needs }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var Needs = ({ node }) => /* @__PURE__ */ jsxs("div", { className: "d-needs", children: [
  /* @__PURE__ */ jsxs("div", { className: "d-needs-head", children: [
    /* @__PURE__ */ jsx("span", { children: "words only \u2014 proposed structure" }),
    /* @__PURE__ */ jsx("b", { children: node.name })
  ] }),
  /* @__PURE__ */ jsx("p", { className: "d-needs-why", children: node.why }),
  /* @__PURE__ */ jsx("span", { className: "d-needs-q", children: node.question }),
  /* @__PURE__ */ jsx("code", { className: "d-needs-shape", children: (node.shape ?? []).join(" \xB7 ") }),
  /* @__PURE__ */ jsxs("div", { className: "d-needs-ex", children: [
    /* @__PURE__ */ jsx("i", { children: "would read" }),
    /* @__PURE__ */ jsx("em", { children: node.example })
  ] }),
  /* @__PURE__ */ jsx(Each, { nodes: node.missing }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var CommandSet = ({ node }) => /* @__PURE__ */ jsxs("div", { className: "d-cmd", children: [
  /* @__PURE__ */ jsxs("div", { className: "d-cmd-head", children: [
    /* @__PURE__ */ jsx("code", { children: node.prefix }),
    /* @__PURE__ */ jsx("span", { children: "over" }),
    /* @__PURE__ */ jsx(Draw, { node: node.over })
  ] }),
  /* @__PURE__ */ jsx("div", { className: "d-cmd-body", children: /* @__PURE__ */ jsx(Each, { nodes: node.verbs }) }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var Verb = ({ node }) => /* @__PURE__ */ jsxs("div", { className: "d-verb", children: [
  /* @__PURE__ */ jsx("code", { children: node.id }),
  /* @__PURE__ */ jsxs("span", { children: [
    /* @__PURE__ */ jsx("i", { children: "takes" }),
    node.takes
  ] }),
  /* @__PURE__ */ jsxs("span", { children: [
    /* @__PURE__ */ jsx("i", { children: "returns" }),
    node.returns
  ] }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var Decision = ({ node }) => /* @__PURE__ */ jsxs("div", { className: `d-decision s-${String(node.state).toLowerCase()}`, children: [
  /* @__PURE__ */ jsxs("div", { className: "d-decision-head", children: [
    /* @__PURE__ */ jsx("span", { className: "d-state", children: node.state }),
    /* @__PURE__ */ jsx("span", { className: "d-ptype", children: node.payload?.type ?? "\u2014" }),
    /* @__PURE__ */ jsx("code", { className: "d-slug", children: node.slug })
  ] }),
  node.badState && /* @__PURE__ */ jsx(Draw, { node: node.badState }),
  /* @__PURE__ */ jsx("div", { className: "d-decision-body", children: /* @__PURE__ */ jsx(Draw, { node: node.payload }) }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var DecisionGroup = ({ node }) => /* @__PURE__ */ jsxs("div", { className: `d-group${node.decisions.length ? "" : " is-empty"}`, children: [
  /* @__PURE__ */ jsxs("div", { className: "d-group-head", children: [
    /* @__PURE__ */ jsx("b", { children: node.name }),
    /* @__PURE__ */ jsx("span", { children: node.question }),
    /* @__PURE__ */ jsxs("i", { className: "d-count", children: [
      node.decisions.length || "no",
      " decisions"
    ] })
  ] }),
  node.decisions.length ? /* @__PURE__ */ jsx(Each, { nodes: node.decisions }) : /* @__PURE__ */ jsx("p", { className: "d-group-empty", children: "This band is declared and holds nothing. Either a decision belongs here and has not been written, or the band should not exist." }),
  /* @__PURE__ */ jsx(Each, { nodes: node.extra })
] });
var Section = ({ node }) => /* @__PURE__ */ jsxs("section", { className: "d-section", children: [
  /* @__PURE__ */ jsx("h2", { children: node.title }),
  /* @__PURE__ */ jsx("p", { className: "d-lede", children: node.question }),
  /* @__PURE__ */ jsx(Each, { nodes: node.body })
] });
var Field = ({ node }) => /* @__PURE__ */ jsxs("div", { className: `d-reqrow${node.value ? "" : " is-missing"}`, children: [
  /* @__PURE__ */ jsx("span", { className: "d-reqname", children: node.name }),
  node.value ? /* @__PURE__ */ jsx("span", { className: "d-reqval", children: node.value }) : /* @__PURE__ */ jsx("span", { className: "d-reqval is-none", children: "not written" })
] });
var Verdict = ({ node }) => /* @__PURE__ */ jsxs("div", { className: `d-verdict${node.blocks.length ? " is-blocked" : " is-clear"}`, children: [
  /* @__PURE__ */ jsx("b", { children: node.blocks.length ? `Not signable \u2014 ${node.blocks.join(", ")}` : "Nothing blocks signing." }),
  node.undescribed > 0 && /* @__PURE__ */ jsxs("p", { className: "d-verdict-err", children: [
    node.undescribed,
    " ",
    node.undescribed === 1 ? "thing" : "things",
    " on this page could not be described. Each is marked in red where it sits."
  ] })
] });
var Document = ({ node }) => /* @__PURE__ */ jsxs(Fragment, { children: [
  /* @__PURE__ */ jsxs("header", { className: "d-top", children: [
    /* @__PURE__ */ jsx("p", { className: "d-eyebrow", children: node.eyebrow }),
    /* @__PURE__ */ jsx("h1", { children: node.title }),
    node.sub && /* @__PURE__ */ jsx("p", { className: "d-sub", children: node.sub })
  ] }),
  /* @__PURE__ */ jsx(Each, { nodes: node.body }),
  /* @__PURE__ */ jsx("footer", { className: "d-foot", children: node.footer })
] });
var DEVICES = {
  undescribed: Undescribed,
  undesigned: Undesigned,
  reference: Reference,
  process: Process,
  step: Step,
  researchNeed: ResearchNeed,
  question: Question,
  gate: Gate,
  gateCondition: GateCondition,
  element: Element,
  member: Member,
  relation: Relation,
  relationEnd: RelationEnd,
  constraint: Constraint,
  payload: Payload,
  payloadElsewhere: PayloadElsewhere,
  sentence: Sentence,
  needs: Needs,
  commandSet: CommandSet,
  verb: Verb,
  decision: Decision,
  decisionGroup: DecisionGroup,
  section: Section,
  field: Field,
  verdict: Verdict,
  document: Document,
  // The language's own vocabularies draw as the marks they declare.
  form: KindChip,
  visibility: Reach,
  alteration: Alteration
};

// view/type.mjs
var UNDESCRIBED = "undescribed";
var found = [];
var undescribedFound = () => found;
var resetUndescribed = () => {
  found = [];
};
var undescribed = (why, at2, value) => {
  const node = { kind: UNDESCRIBED, why, at: at2, value };
  found.push(node);
  return node;
};
var rest = (obj, taken, at2) => Object.keys(obj ?? {}).filter((k) => !taken.includes(k)).map((k) => undescribed(`nothing renders the field "${k}"`, at2, obj[k]));
var list = (xs, fn) => (xs ?? []).map(fn);
var tag = (kind, spec) => spec ? { kind, ...spec } : null;
var reference = (t, at2, defined, params = []) => {
  if (t == null) return undescribed("a reference with no name", at2, t);
  const r = typeof t === "string" ? { name: t } : t;
  if (!r.name) return undescribed("a reference with no name", at2, t);
  const args = list(r.arguments, (x, i) => reference(x, `${at2}.arguments[${i}]`, defined, params));
  const what = params.includes(r.name) ? "parameter" : defined(r.name) ? "defined" : vocab.primitive[r.name] ? "primitive" : vocab.external[r.name] ? "external" : null;
  if (!what) {
    return {
      kind: "reference",
      name: r.name,
      resolves: null,
      arguments: args,
      gap: undescribed(`"${r.name}" names nothing`, at2, r.name)
    };
  }
  return {
    kind: "reference",
    name: r.name,
    resolves: what,
    arguments: args,
    extra: rest(r, ["name", "arguments"], at2)
  };
};
var gateCondition = (c, at2) => {
  const spec = vocab.gateCondition[c?.kind];
  if (!spec) return undescribed(`"${c?.kind}" is not a declared gate condition kind`, at2, c);
  const missing = (spec.shape ?? []).filter((f) => !c[f]);
  return {
    kind: "gateCondition",
    spec,
    over: c.over,
    by: c.by,
    missing: missing.map((f) => undescribed(`a ${spec.id} condition must say "${f}"`, at2, c)),
    extra: rest(c, ["kind", ...spec.shape ?? []], at2)
  };
};
var gate = (g, at2) => {
  const spec = vocab.gateKind[g?.kind];
  if (!spec) return undescribed(`"${g?.kind}" is not a declared gate kind`, at2, g);
  const conditions = list(g.conditions, (c, i) => gateCondition(c, `${at2}.conditions[${i}]`));
  return {
    kind: "gate",
    spec,
    conditions,
    empty: conditions.length ? null : undescribed("a gate with no conditions checks nothing", at2, g),
    extra: rest(g, ["kind", "conditions"], at2)
  };
};
var step = (s, at2, defined) => {
  const actor = vocab.actor[s?.actor];
  if (!actor) return undescribed(`"${s?.actor}" is not a declared actor`, at2, s);
  return {
    kind: "step",
    actor,
    name: s.name,
    note: s.note,
    fansOut: s.fans_out,
    takes: list(s.takes, (t, i) => reference(t, `${at2}.takes[${i}]`, defined)),
    produces: list(s.produces, (t, i) => reference(t, `${at2}.produces[${i}]`, defined)),
    unnamed: s.name ? null : undescribed("a step with no name", at2, s),
    extra: rest(s, ["actor", "name", "note", "fans_out", "takes", "produces"], at2)
  };
};
var researchNeed = (n, at2) => {
  const spec = vocab.research[n?.kind];
  if (!spec) return undescribed(`"${n?.kind}" is not a declared research kind`, at2, n);
  return {
    kind: "researchNeed",
    spec,
    required: Boolean(n.required),
    hasProcess: Boolean(spec.process),
    extra: rest(n, ["kind", "required"], at2)
  };
};
var question = (q, at2) => ({
  kind: "question",
  id: q?.id,
  text: q?.text,
  answeredBy: list(q?.answered_by, (k, i) => vocab.research[k] ?? undescribed(`"${k}" is not a declared research kind`, `${at2}.answered_by[${i}]`, k)),
  unanswerable: (q?.answered_by ?? []).length ? null : undescribed("a question with no research behind it cannot be answered", at2, q),
  extra: rest(q, ["id", "text", "answered_by"], at2)
});
var process = (p, at2, defined) => {
  const members = list(p?.members, (x, i) => x?.actor ? step(x, `${at2}.members[${i}]`, defined) : process(x, `${at2}.members[${i}]`, defined));
  const research = list(p?.research, (r, i) => researchNeed(r, `${at2}.research[${i}]`));
  const questions = list(p?.questions, (q, i) => question(q, `${at2}.questions[${i}]`));
  const gates = list(p?.gates, (g, i) => gate(g, `${at2}.gates[${i}]`));
  return {
    kind: "process",
    name: p?.name,
    note: p?.note,
    takes: list(p?.takes, (t, i) => reference(t, `${at2}.takes[${i}]`, defined)),
    produces: list(p?.produces, (t, i) => reference(t, `${at2}.produces[${i}]`, defined)),
    research,
    questions,
    gates,
    members,
    undesigned: members.length || research.length || questions.length || gates.length ? null : { kind: "undesigned", name: p?.name },
    extra: rest(p, ["name", "note", "takes", "produces", "research", "questions", "gates", "members"], at2)
  };
};
var typeMember = (x, at2, defined, params = []) => {
  const mk = vocab.member[x?.kind];
  const vis = vocab.visibility[x?.visibility];
  const alt = vocab.alteration[x?.alteration];
  const bad = [];
  if (!mk) bad.push(undescribed(`"${x?.kind}" is not a declared member kind`, at2, x?.kind));
  if (!vis) bad.push(undescribed(`"${x?.visibility}" is not a declared visibility`, at2, x?.visibility));
  if (x?.alteration && !alt) bad.push(undescribed(`"${x.alteration}" is not a declared alteration`, at2, x.alteration));
  return {
    kind: "member",
    name: x?.name,
    memberKind: mk,
    visibility: tag("visibility", vis),
    alteration: tag("alteration", alt),
    note: x?.note,
    type: x?.type == null ? null : reference(x.type, `${at2}.type`, defined, params),
    bad,
    extra: rest(x, ["name", "kind", "visibility", "alteration", "note", "type"], at2)
  };
};
var element = (e, at2, defined) => {
  const form = vocab.form[e?.form];
  const vis = vocab.visibility[e?.visibility];
  const alt = vocab.alteration[e?.alteration];
  const bad = [];
  if (!form) bad.push(undescribed(`"${e?.form}" is not a declared form`, at2, e?.form));
  if (!vis) bad.push(undescribed(`"${e?.visibility}" is not a declared visibility`, at2, e?.visibility));
  if (!alt) bad.push(undescribed(`"${e?.alteration}" is not a declared alteration`, at2, e?.alteration));
  if (e?.storage && !vocab.storage[e.storage]) bad.push(undescribed(`"${e.storage}" is not a declared storage family`, at2, e.storage));
  if (e?.precision && !vocab.precision[e.precision]) bad.push(undescribed(`"${e.precision}" is not a declared precision`, at2, e.precision));
  return {
    kind: "element",
    id: e?.id,
    name: e?.name,
    form: tag("form", form),
    visibility: tag("visibility", vis),
    alteration: tag("alteration", alt),
    parameters: e?.parameters ?? [],
    purpose: e?.purpose,
    generated: Boolean(e?.generated),
    storage: vocab.storage[e?.storage],
    precision: vocab.precision[e?.precision],
    members: list(e?.members, (x, i) => typeMember(x, `${at2}.members[${i}]`, defined, e?.parameters ?? [])),
    bad,
    extra: rest(e, [
      "id",
      "name",
      "form",
      "visibility",
      "alteration",
      "parameters",
      "purpose",
      "generated",
      "storage",
      "precision",
      "members"
    ], at2)
  };
};
var payload = (p, at2, defined) => {
  if (!p?.type) return undescribed("a payload with no type", at2, p);
  if (!payloadKinds.includes(p.type)) {
    return undescribed(`"${p.type}" is not declared as a Payload in the architecture`, at2, p.type);
  }
  if (p.in) {
    return {
      kind: "payloadElsewhere",
      type: p.type,
      in: p.in,
      extra: rest(p, ["type", "in"], at2)
    };
  }
  if (p.type === "Process") {
    const { type, ...body } = p;
    return { kind: "payload", type: p.type, body: process(body, at2, defined) };
  }
  if (p.type === "Sentence") {
    const needs = p.needs;
    const want = ["name", "question", "shape", "example", "why"];
    return {
      kind: "payload",
      type: p.type,
      body: {
        kind: "sentence",
        text: p.text,
        needs: needs ? {
          kind: "needs",
          ...needs,
          missing: want.filter((f) => !needs[f]).map((f) => undescribed(`a sentence must say needs.${f}`, at2, needs)),
          extra: rest(needs, want, `${at2}.needs`)
        } : undescribed("a sentence must say what would replace it", at2, p),
        extra: rest(p, ["type", "text", "needs"], at2)
      }
    };
  }
  if (p.type === "CommandSet") {
    return {
      kind: "payload",
      type: p.type,
      body: {
        kind: "commandSet",
        prefix: p.prefix,
        over: reference(p.over, `${at2}.over`, defined),
        verbs: list(p.verbs, (v, i) => ({
          kind: "verb",
          ...v,
          extra: rest(v, ["id", "takes", "returns"], `${at2}.verbs[${i}]`)
        })),
        extra: rest(p, ["type", "prefix", "over", "verbs"], at2)
      }
    };
  }
  return undescribed(`no rule types a ${p.type} payload`, at2, p);
};
var decision = (d, at2, states, defined) => {
  const stateOk = states.includes(d?.state);
  return {
    kind: "decision",
    slug: d?.slug,
    group: d?.group,
    state: d?.state,
    badState: stateOk ? null : undescribed(`"${d?.state}" is not a declared decision state`, at2, d?.state),
    payload: payload(d?.payload, `${at2}.payload`, defined),
    extra: rest(d, ["slug", "group", "state", "payload"], at2)
  };
};

// language-types.mjs
var enumOf = (id, purpose, items) => ({
  id,
  name: id,
  form: "enum",
  visibility: "public",
  alteration: "inserted",
  generated: true,
  purpose,
  members: items.map((x) => ({
    name: x.id,
    kind: "value",
    visibility: "public",
    note: x.name ?? "",
    alteration: "inserted"
  }))
});
var field = (name, type) => ({
  name,
  kind: "field",
  visibility: "public",
  type,
  alteration: "inserted"
});
var recordOf = (id, purpose, members) => ({
  id,
  name: id,
  form: "record",
  visibility: "public",
  alteration: "inserted",
  generated: true,
  purpose,
  members
});
var list2 = (of) => ({ name: "IReadOnlyList", arguments: [{ name: of }] });
var partsOf = (device, label, types, extra = []) => {
  const parts = device.parts ?? [];
  const out = [];
  for (const p of parts) {
    const spec = types[p.id];
    if (!spec) throw new Error(`language-types: ${label} part "${p.id}" has no type`);
    out.push(field(spec.name ?? p.id, spec.type));
  }
  return [...extra, ...out];
};
var languageTypes = (m) => [
  enumOf("Form", "What a thing is.", m.forms),
  enumOf("Visibility", "How far out a thing can be seen.", m.visibility),
  enumOf("Alteration", "What a proposal does to a thing.", m.alteration),
  enumOf("ConstraintKind", "Which shape a constraint has.", m.constraint_kinds),
  enumOf("PermissionVerb", "What an actor may do.", m.permission_verbs),
  enumOf("Enforcement", "Whether anything checks a constraint.", m.constraint_enforcement ?? [
    { id: "sentence", name: "Nothing checks it" },
    { id: "mechanical", name: "The Rule phase made it a check" }
  ]),
  enumOf("Actor", "Who can run a step.", m.actors),
  enumOf("ResearchKind", "What a process can be required to find out.", m.research),
  enumOf("Lens", "How a research kind looks for something.", m.lenses),
  enumOf("GateKind", "When a gate is checked.", m.gate_kinds),
  enumOf("GateConditionKind", "What a gate condition checks.", m.gate_condition_kinds),
  recordOf(
    "TypeReference",
    "How one element names another, anywhere a type appears.",
    partsOf(m.type_reference, "type_reference", {
      name: { type: "string" },
      arguments: { type: list2("TypeReference") }
    })
  ),
  recordOf("ResearchNeed", "One kind of research a process needs, and whether it may be skipped.", [
    field("kind", { name: "ResearchKind" }),
    field("required", "bool")
  ]),
  recordOf("Question", "Something a process must answer, and the research that answers it.", [
    field("id", "string"),
    field("text", "string"),
    field("answeredBy", list2("ResearchKind"))
  ]),
  {
    id: "ProcessMember",
    name: "ProcessMember",
    form: "interface",
    visibility: "public",
    alteration: "inserted",
    generated: true,
    purpose: "A member of a process. A Step when one actor does it, a Process when it has members of its own.",
    members: []
  },
  recordOf("GateCondition", "One thing a gate checks, and what satisfies it.", [
    field("kind", { name: "GateConditionKind" }),
    field("over", "string"),
    field("by", "string")
  ]),
  recordOf(
    "Gate",
    "What must hold before a process may start, or before it may finish.",
    partsOf(m.gate, "gate", {
      kind: { type: { name: "GateKind" } },
      conditions: { type: list2("GateCondition") }
    })
  ),
  recordOf(
    "Step",
    "One unit of work inside a process, run by exactly one actor.",
    partsOf(m.step, "step", {
      actor: { type: { name: "Actor" } },
      takes: { type: list2("TypeReference") },
      produces: { type: list2("TypeReference") },
      fans_out: { name: "fansOut", type: "string" }
    }, [field("name", "string")])
  ),
  recordOf(
    "Process",
    "A named, ordered sequence whose members are steps or processes.",
    partsOf(m.process, "process", {
      takes: { type: list2("TypeReference") },
      produces: { type: list2("TypeReference") },
      research: { type: list2("ResearchNeed") },
      questions: { type: list2("Question") },
      gates: { type: list2("Gate") }
    }, [field("name", "string"), field("members", list2("ProcessMember"))])
  ),
  recordOf("Verb", "One operation in a command set, with what it takes and returns.", [
    field("id", "string"),
    field("takes", "string"),
    field("returns", "string")
  ]),
  recordOf(
    "CommandSet",
    "The commands that read and change one part of a record.",
    partsOf(m.command_set, "command_set", {
      over: { type: { name: "TypeReference" } },
      prefix: { type: "string" },
      verb: { name: "verbs", type: list2("Verb") }
    })
  ),
  recordOf(
    "DecisionGroup",
    "An ordered band of decisions, named by the question it answers.",
    partsOf(m.decision_group, "decision_group", {
      id: { type: "string" },
      name: { type: "string" },
      question: { type: "string" }
    })
  ),
  recordOf("Element", "One named thing in an architecture, with where it lives and what it is.", [
    field("id", "string"),
    field("name", "string"),
    field("form", { name: "Form" }),
    field("visibility", { name: "Visibility" }),
    field("unit", "string"),
    field("scope", "string"),
    field("alteration", { name: "Alteration" })
  ]),
  recordOf("Relation", "A directed link between two elements, or between their members.", [
    field("kind", "string"),
    field("from", "string"),
    field("to", "string"),
    field("alteration", { name: "Alteration" })
  ]),
  recordOf("Constraint", "A signed statement about the work, and whether anything checks it.", [
    field("kind", { name: "ConstraintKind" }),
    field("text", "markdown"),
    field("enforcement", { name: "Enforcement" })
  ])
];

// view/page.jsx
import { Fragment as Fragment2, jsx as jsx2, jsxs as jsxs2 } from "react/jsx-runtime";
var contractDocument = () => {
  resetUndescribed();
  const elements = [...architecture.elements, ...languageTypes(language)];
  const names = new Set(elements.map((e) => e.name));
  const defined = (n) => names.has(n);
  const contractRecord = elements.find((e) => e.id === "Contract");
  const states = elements.find((e) => e.id === "DecisionState").members.map((m) => m.name);
  const decisions = (contract.decisions ?? []).map((d, i) => decision(d, `contract.decisions[${i}]`, states, defined));
  const fields = contractRecord.members.filter((f) => f.name !== "decisions").map((f) => ({ kind: "field", name: f.name, value: contract[f.name] }));
  const groups = (contract.decision_groups ?? []).map((g) => ({
    kind: "decisionGroup",
    id: g.id,
    name: g.name,
    question: g.question,
    decisions: decisions.filter((d) => d.group === g.id),
    extra: []
  }));
  const orphans = decisions.filter((d) => !(contract.decision_groups ?? []).some((g) => g.id === d.group));
  const blocks = [];
  const unwritten = fields.filter((f) => !f.value);
  if (unwritten.length) blocks.push(`${unwritten.length} required field${unwritten.length === 1 ? "" : "s"} not written`);
  const proposed = decisions.filter((d) => d.state === "Proposed");
  if (proposed.length) blocks.push(`${proposed.length} decision${proposed.length === 1 ? "" : "s"} still proposed`);
  const wordsOnly = decisions.filter((d) => d.payload?.body?.kind === "sentence");
  if (wordsOnly.length) blocks.push(`${wordsOnly.length} decision${wordsOnly.length === 1 ? "" : "s"} are words only`);
  const run = decisions.find((d) => d.payload?.body?.kind === "process");
  const phases = run?.payload?.body?.members ?? [];
  const undone = phases.filter((p) => p.undesigned);
  if (undone.length) blocks.push(`${undone.length} of ${phases.length} phases undesigned`);
  const body = [
    {
      kind: "section",
      title: "What this run covers",
      question: "What is settled, and what has not been written?",
      body: fields
    },
    {
      kind: "section",
      title: "The decisions",
      question: "What has the human agreed to, and what is still waiting?",
      body: [...groups, ...orphans]
    }
  ];
  const doc = {
    kind: "document",
    eyebrow: `AgentGuard \xB7 issue ${contract.issue}`,
    title: contract.slug,
    footer: "Generated from contract.yaml. The YAML is the source; never edit the HTML.",
    body
  };
  const definitions = [
    ...elements.map((e) => ({ name: e.name, node: element(e, `architecture.${e.id}`, defined) })),
    ...(language.research ?? []).filter((r) => r.process).map((r) => ({ name: r.name, node: process(r.process, `language.research.${r.id}`, defined) }))
  ];
  const verdict = { kind: "verdict", blocks, undescribed: undescribedFound().length };
  doc.body = [verdict, ...body];
  return { doc, definitions, undescribed: undescribedFound(), blocks };
};
var renderContract = () => {
  const { doc, definitions, undescribed: undescribed2, blocks } = contractDocument();
  return {
    element: /* @__PURE__ */ jsxs2(Fragment2, { children: [
      /* @__PURE__ */ jsx2(Draw, { node: doc }),
      /* @__PURE__ */ jsx2("div", { id: "defs", children: definitions.map((d) => /* @__PURE__ */ jsx2("div", { "data-defname": d.name, children: /* @__PURE__ */ jsx2(Draw, { node: d.node }) }, d.name)) })
    ] }),
    undescribed: undescribed2,
    blocks
  };
};
export {
  contractDocument,
  renderContract
};
