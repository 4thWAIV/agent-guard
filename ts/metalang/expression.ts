// A regular expression written in a file: reading one, and matching a value against it.
//
// It is its own file because everything that judges a value against an expression uses it: the
// constraint logic today, and whatever else comes to need one. Nothing here reaches the file
// system, so the page's bundle may import it.
//
// What a regular expression IS belongs to the concrete syntax rather than to the vocabulary. The
// language says a pattern holds an expression; this says what the text written there means.
//
// A pattern says what the WHOLE value is, so a match is always of the whole value. A pattern that
// matched a part would make `[a-z]` accept every id that happens to contain a letter.
//
// Not done: which regular expressions are allowed. A subset means the same thing in every runtime
// and the whole of neither does, so an expression that reads here may still mean something else
// where the document is used. Nothing checks that yet.

/** A regular expression that has been read, and what it matches. */
export interface Expression {
  /** The text it was written as. */
  readonly source: string
  /** Whether the whole of the value is what it matches. */
  readonly matches: (value: string) => boolean
}

/** A regular expression, or what stopped it being read. One of the two is always nothing. */
export interface ReadExpression {
  readonly expression: Expression | null
  readonly fault: string | null
}

/**
 * Reads the text as a regular expression. It reports rather than throws, so a document carrying
 * a broken one is reported like anything else that is wrong with it.
 */
export function readExpression(source: unknown): ReadExpression {
  if (typeof source !== 'string') return { expression: null, fault: 'must be written as text' }

  let whole: RegExp
  try {
    whole = new RegExp(`^(?:${source})$`, 'u')
  } catch (e) {
    const message = e instanceof Error ? e.message : String(e)
    return { expression: null, fault: `is not a regular expression: ${message}` }
  }

  const matches = (value: string): boolean => {
    const found = whole.exec(value)
    return found !== null && found[0] === value
  }
  return { expression: { source, matches }, fault: null }
}
