import { describe, expect, it } from 'vitest';
import { applyFavicon, ITSC_FAVICON } from './tenant.service';

describe('applyFavicon', () => {
  it('cambia los tres íconos de la pestaña', () => {
    const doc = document.implementation.createHTMLDocument('t');
    doc.head.innerHTML = `
      <link rel="icon" type="image/svg+xml" href="favicon.svg">
      <link rel="alternate icon" type="image/x-icon" href="favicon.ico">
      <link rel="apple-touch-icon" href="apple-touch-icon.png">`;

    applyFavicon(ITSC_FAVICON, doc);

    expect(doc.querySelector('link[rel="icon"]')?.getAttribute('href')).toBe('favicon-itsc.svg');
    expect(doc.querySelector('link[rel="alternate icon"]')?.getAttribute('href')).toBe('favicon-itsc.ico');
    expect(doc.querySelector('link[rel="apple-touch-icon"]')?.getAttribute('href')).toBe('apple-touch-icon-itsc.png');
  });
});
