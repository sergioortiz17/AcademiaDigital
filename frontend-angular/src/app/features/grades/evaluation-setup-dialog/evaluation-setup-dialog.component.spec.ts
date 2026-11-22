import { beforeEach, describe, expect, it, vi } from 'vitest';
import { EvaluationSetupDialogComponent, distributeWeights } from './evaluation-setup-dialog.component';

describe('distributeWeights', () => {
  it('splits 100% evenly and always adds up to exactly 100', () => {
    expect(distributeWeights(4)).toEqual([25, 25, 25, 25]);
    expect(distributeWeights(3)).toEqual([33.34, 33.33, 33.33]);
    for (let n = 1; n <= 20; n++) {
      const cents = distributeWeights(n).reduce((sum, w) => sum + Math.round(w * 100), 0);
      expect(cents).toBe(10000);
    }
  });
});

describe('Evaluation setup without manual weights', () => {
  let component: EvaluationSetupDialogComponent;
  let close: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    close = vi.fn();
    type Args = ConstructorParameters<typeof EvaluationSetupDialogComponent>;
    component = new EvaluationSetupDialogComponent(
      { close } as unknown as Args[0],
      { detectChanges: vi.fn() } as unknown as Args[1]
    );
  });

  it('gives recoveries no weight and splits the rest evenly', () => {
    component.confirm();
    const sent = close.mock.calls[0][0] as { name: string; weightPercentage: number; isRecovery: boolean }[];
    expect(sent.filter(e => e.isRecovery).every(e => e.weightPercentage === 0)).toBe(true);
    expect(sent.filter(e => !e.isRecovery).map(e => e.weightPercentage)).toEqual([25, 25, 25, 25]);
  });

  it('redistributes when an instance becomes a recovery', () => {
    component.evaluations[0].isRecovery = true;
    component.onRecoveryToggle();
    expect(component.weights.filter(w => w > 0)).toEqual([33.34, 33.33, 33.33]);
  });

  it('rejects duplicated names with a clear message', () => {
    component.evaluations[1].name = '1era instancia';
    expect(component.isValid).toBe(false);
    expect(component.validationMessage).toContain('mismo nombre');
    component.confirm();
    expect(close).not.toHaveBeenCalled();
  });

  it('requires four instances for annual courses', () => {
    component.evaluations = component.evaluations.filter(e => e.name !== 'IEFI');
    expect(component.validationMessage).toContain('al menos 4');
  });

  it('requires at least one regular instance', () => {
    component.evaluations.forEach(e => (e.isRecovery = true));
    expect(component.validationMessage).toContain('al menos una instancia');
  });
});
