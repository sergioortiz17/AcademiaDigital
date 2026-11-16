import { describe, expect, it } from 'vitest';
import { platformEntryFor, siteBaseDomain } from './platform-hosts';

describe('platformEntryFor', () => {
  it('lleva la landing y el panel a sus pantallas; el resto, al login', () => {
    expect(platformEntryFor('academiadigital.dryvn.org')).toBe('/landing');
    expect(platformEntryFor('ADMIN.dryvn.org')).toBe('/platform');
    expect(platformEntryFor('itsc.dryvn.org')).toBeNull();
    expect(platformEntryFor('usb.dryvn.org')).toBeNull();
    expect(platformEntryFor('localhost')).toBeNull();
  });

  it('deriva el dominio base para mostrar <slug>.<base>', () => {
    expect(siteBaseDomain('academiadigital.dryvn.org')).toBe('dryvn.org');
    expect(siteBaseDomain('localhost')).toBe('dryvn.org');
  });
});
