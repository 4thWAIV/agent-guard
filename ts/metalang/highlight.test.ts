// What to mark in a render, and where.
//
// The two things these tests are about. A path written any of the ways a person would write it
// reaches the same node, and the page is asked for it by one form. And a path that names nothing
// comes back as naming nothing, because a highlight left behind by an edit has to say so rather
// than disappear.

import { describe, expect, test } from 'bun:test'

import {
  highlightOf,
  isHighlight,
  marksOf,
  nameOf,
  NO_MARKS,
  pathTo,
  stepsOf,
} from './highlight.ts'

const doc = {
  scope: ['arch'],
  title: 'The architecture language',
  declare: [
    {
      entity: {
        id: 'form',
        is: 'object',
        properties: { mark: { is: 'mark' }, name: { is: 'string' } },
        constraints: [{ pattern: { value: 'x' } }],
      },
    },
    { choice: { id: 'mark', options: ['glyph', 'uri'] } },
    {
      enum: {
        id: 'forms',
        is: 'form',
        values: [
          { id: 'interface', value: { mark: '◇', name: 'Interface' } },
          { id: 'process', value: { mark: '▷', name: 'Process' } },
        ],
      },
    },
    { external: { cast: { id: 'to_text', from: 'uri', to: 'string' } } },
  ],
}

describe('the steps a path is made of', () => {
  test('a name is one step', () => {
    expect(stepsOf('title')).toEqual([{ part: 'title' }])
  })

  test('a position is its own step', () => {
    expect(stepsOf('declare[2]')).toEqual([{ part: 'declare' }, { position: '2' }])
  })

  test('a position may be a name', () => {
    expect(stepsOf('declare[forms]')).toEqual([{ part: 'declare' }, { position: 'forms' }])
  })

  test('a name in a position may carry spaces and punctuation', () => {
    expect(stepsOf('values[Accept, change or reject]')).toEqual([
      { part: 'values' },
      { position: 'Accept, change or reject' },
    ])
  })

  test('positions and names mix along one path', () => {
    expect(stepsOf('declare[forms].values[0].value.mark')).toEqual([
      { part: 'declare' },
      { position: 'forms' },
      { part: 'values' },
      { position: '0' },
      { part: 'value' },
      { part: 'mark' },
    ])
  })

  test('nothing is not a path', () => {
    expect(stepsOf('')).toBeNull()
  })

  test('a position left open is not a path', () => {
    expect(stepsOf('declare[2')).toBeNull()
  })
})

describe('the name a node is known by', () => {
  test('its own id', () => {
    expect(nameOf({ id: 'interface' })).toBe('interface')
  })

  test('the id inside the one word it names', () => {
    expect(nameOf({ entity: { id: 'form' } })).toBe('form')
  })

  test('nothing where no id is written', () => {
    expect(nameOf({ entity: { title: 'A form.' } })).toBeNull()
  })

  test('nothing for anything that is not a mapping', () => {
    expect(nameOf('form')).toBeNull()
  })
})

describe('where a path points', () => {
  test('a key at the top level', () => {
    expect(pathTo(doc, 'title')).toBe('title')
  })

  test('a declaration by its position', () => {
    expect(pathTo(doc, 'declare[1]')).toBe('declare[1]')
  })

  test('a declaration by the name it declares', () => {
    expect(pathTo(doc, 'declare[mark]')).toBe('declare[1]')
  })

  test('the kind written out resolves, and the one form carries it', () => {
    expect(pathTo(doc, 'declare[1].choice.options')).toBe('declare[1].choice.options')
  })

  test('the kind left out resolves to the same place', () => {
    expect(pathTo(doc, 'declare[mark].options')).toBe('declare[1].choice.options')
  })

  test('one of an enum\'s values by its name', () => {
    expect(pathTo(doc, 'declare[forms].values[process]')).toBe('declare[2].enum.values[1]')
  })

  test('a part inside a value', () => {
    expect(pathTo(doc, 'declare[forms].values[interface].value.mark')).toBe(
      'declare[2].enum.values[0].value.mark',
    )
  })

  test('one property of a type', () => {
    expect(pathTo(doc, 'declare[form].properties.mark')).toBe('declare[0].entity.properties.mark')
  })

  test('one constraint of a type', () => {
    expect(pathTo(doc, 'declare[form].constraints[0]')).toBe('declare[0].entity.constraints[0]')
  })

  test('a declaration written inside external carries both words', () => {
    expect(pathTo(doc, 'declare[to_text].from')).toBe('declare[3].external.cast.from')
  })

  test('a name no sibling carries points nowhere', () => {
    expect(pathTo(doc, 'declare[nothing]')).toBeNull()
  })

  test('a position past the end points nowhere', () => {
    expect(pathTo(doc, 'declare[9]')).toBeNull()
  })

  test('a key nothing carries points nowhere', () => {
    expect(pathTo(doc, 'declare[form].colour')).toBeNull()
  })

  test('a position where there is no list points nowhere', () => {
    expect(pathTo(doc, 'title[0]')).toBeNull()
  })

  test('a path that is not one points nowhere', () => {
    expect(pathTo(doc, '')).toBeNull()
  })
})

describe('what a highlight file says', () => {
  const file = {
    of: 'arch.yaml',
    highlight: [
      { path: 'declare[mark].options', note: 'Why the first option is a glyph.' },
      { path: 'declare[form].properties.mark' },
    ],
  }

  test('it is a highlight when it says which file it is about', () => {
    expect(isHighlight(file)).toBe(true)
  })

  test('a document is not a highlight', () => {
    expect(isHighlight(doc)).toBe(false)
  })

  test('the file it is about, and its entries', () => {
    const read = highlightOf(file)
    expect(read.of).toBe('arch.yaml')
    expect(read.entries).toHaveLength(2)
    expect(read.wrong).toEqual([])
  })

  test('an entry with no note still points', () => {
    expect(highlightOf(file).entries[1]).toEqual({
      path: 'declare[form].properties.mark',
      note: '',
    })
  })

  test('saying no file is reported', () => {
    expect(highlightOf({ highlight: [] }).wrong[0]).toContain('names the file this highlights')
  })

  test('holding nothing to highlight is reported', () => {
    expect(highlightOf({ of: 'a.yaml' }).wrong[0]).toContain('holds what is highlighted')
  })

  test('a list written as anything else is reported', () => {
    expect(highlightOf({ of: 'a.yaml', highlight: 'one' }).wrong[0]).toContain('written as a list')
  })

  test('an entry that is not a mapping is reported and the rest are kept', () => {
    const read = highlightOf({ of: 'a.yaml', highlight: ['x', { path: 'title' }] })
    expect(read.wrong[0]).toContain('highlight[0] carries a path and a note')
    expect(read.entries).toHaveLength(1)
  })

  test('an entry with no path is reported', () => {
    const read = highlightOf({ of: 'a.yaml', highlight: [{ note: 'look' }] })
    expect(read.wrong[0]).toContain('highlight[0].path')
    expect(read.entries).toEqual([])
  })

  test('a note that is not text is reported', () => {
    const read = highlightOf({ of: 'a.yaml', highlight: [{ path: 'title', note: 7 }] })
    expect(read.wrong[0]).toContain('highlight[0].note')
  })

  test('anything that is not a mapping is not a highlight at all', () => {
    expect(highlightOf(['of']).wrong).toEqual(['a highlight is a mapping'])
  })
})

describe('the marks a document carries', () => {
  test('each entry marks where it points, with what it says', () => {
    const marks = marksOf(doc, [
      { path: 'declare[mark].options', note: 'Distinguishable.' },
      { path: 'declare[forms].values[process]', note: '' },
    ])
    expect([...marks.marked]).toEqual([
      ['declare[1].choice.options', 'Distinguishable.'],
      ['declare[2].enum.values[1]', ''],
    ])
    expect(marks.missing).toEqual([])
  })

  test('a path that names nothing is reported as written, not dropped', () => {
    const marks = marksOf(doc, [{ path: 'declare[gone].options', note: 'stale' }])
    expect(marks.marked.size).toBe(0)
    expect(marks.missing).toEqual(['declare[gone].options'])
  })

  test('two entries pointing at one node leave the last word', () => {
    const marks = marksOf(doc, [
      { path: 'title', note: 'first' },
      { path: 'title', note: 'second' },
    ])
    expect(marks.marked.get('title')).toBe('second')
  })

  test('no entries mark nothing', () => {
    expect(marksOf(doc, []).marked.size).toBe(0)
  })

  test('nothing marked is what a document draws with no highlight', () => {
    expect(NO_MARKS.marked.size).toBe(0)
    expect(NO_MARKS.missing).toEqual([])
  })
})
