# Ledger — every word the language is allowed to use

Rule: no word may be used in any model file unless it appears below with Tim's yes and a date.
A word proposed and not yet ruled on sits under PENDING. Nothing else exists.

## Approved

| word | what it holds | approved |
|------|---------------|----------|
| null | the general form's primitive for no value | 2026-10-13 |
| boolean | the general form's primitive for true or false | 2026-10-13 |
| object | the general form's primitive for a thing with named parts | 2026-10-13 |
| collection | the general form's primitive for every collection kind | 2026-10-13 |
| number | the general form's primitive for a number | 2026-10-13 |
| string | the general form's primitive for a run of characters | 2026-10-13 |

These six are superseded. On 2026-10-13 Tim replaced the split between primitives and the scalars
built on them with one set of real types, and `collection` became an abstract type with `roster`,
`list`, `set` and `array` extending it.

`null` must be written quoted in a YAML file. YAML 1.2 reads a bare `null` as the null value rather
than as the word.

| word | what it holds | approved |
|------|---------------|----------|
| instant | the shape of a point in time | 2026-10-13 |
| duration | the shape of a difference between two instants | 2026-10-13 |
| regex | the shape of a regular expression | 2026-10-13 |
| uuid | the shape of a universally unique identifier | 2026-10-13 |

These four are the whole of `shaped_as`. `instant` replaces `datetime`, and `uuid` is taken over
`guid`, which names the same thing in Microsoft's ecosystem only.

## Approved, not yet worded

These are concepts Tim has settled. They have no ratified word, so they may not appear in a
model file until one is chosen.

| concept | word | settled |
|---------|------|---------|
| ordered, unique | list | 2026-10-15 |
| ordered, duplicates allowed | array | 2026-10-15 |
| unordered, unique | set | 2026-10-09 |
| unordered, duplicates allowed | bag | 2026-10-09 |

All four are named, each by the word that already means it. `roster` is dropped: it was a coined word
for the ordered and unique kind, which is what a list is.

Tim named `queue` and `stack` as kinds to add later.

## In use in metalang.yaml, pending Tim's confirmation

Tim said "let's try that out and see if it works" on 2026-10-11, so these are written and
working, not yet ratified.

| word | what it holds |
|------|---------------|
| is | which metaclass a definition is |
| title / description | the prose |
| abstract | whether a definition exists only to be extended |
| extends | the entities a definition is a kind of |
| identified_by | which part carries an instance's identity |
| properties | the named parts of an entity |
| closed | whether parts not named are refused |
| required | whether a part must be present |
| order / unique / unique_by | the collection mechanics |
| min_items / max_items | how many members |
| type / default | a field's value type and fallback |
| pattern / one_of / minimum / maximum / min_length / max_length | the value requirements |
| and / or / exactly_one / not | combining requirements |
| target / owns / opposite | a link's destination, ownership and paired end |
| of / shaped_as | a scalar's underlying primitive and format |
| options / defines | what a choice and a document own |
| significant / not_significant | the two orderings |
| yes / no | the two truths |

Metaclasses: entity, part, requirement, field, link, scalar, choice, option, document.

## Held, to return to

| word | state |
|------|-------|
| hash | a specialised string. Tim may treat it as over-engineering and will return to it. What it constrains is undefined. |
| email | undecided. To be added if a use appears. |
| host | undecided. To be added if a use appears. The standard's word is hostname. |
| url | undecided. To be added if a use appears. The standard's word is uri. |

## Proposed and awaiting a ruling

The exact form has no approved words. Tim called the Kaitai Struct vocabulary recorded on 2026-10-10
more than we need and a good start. The narrowed proposal of 2026-10-13 is thirteen primitives —
u1 u2 u4 u8, s1 s2 s4 s8, f4 f8, bytes and str — with one document-level `endian` declaration. It
needs two keywords that do not exist: a byte length on a part, and an encoding on a `str`.

## Rejected

The `scope` row previously here was wrong and is removed. `scope` is the word for a namespace,
declared in language.yaml's containment ladder on 2026-10-07.


| word | why |
|------|-----|
| device | from the discarded design |
| part | from the discarded design |
| part_absence | from the discarded design; Tim called the notation strained |
| holding | coined without approval |
| holdable | coined without approval |
| tone | coined without approval |
| self | coined without approval |
| glyph | coined without approval |
| undesigned | invented to hide holes behind an optional |
| one-of / any-of | two words used for one operation (union) |
| date_time | replaced by instant |
| guid | uuid names the same thing and is the name outside Microsoft's ecosystem |
| date / time / idn_email / idn_hostname / ipv4 / ipv6 / uri_reference / iri / iri_reference / uri_template / json_pointer / relative_json_pointer | cut from the formats on 2026-10-13; no use established |
