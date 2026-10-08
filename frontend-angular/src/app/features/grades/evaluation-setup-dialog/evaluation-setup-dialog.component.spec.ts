import { beforeEach, describe, expect, it, vi } from 'vitest';
import { EvaluationSetupDialogComponent } from './evaluation-setup-dialog.component';

describe('Evaluation recovery weight', () => {
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

  function toggle(index: number, recovery: boolean): void {
    const evaluation = component.evaluations[index];
    evaluation.isRecovery = recovery;
    component.onRecoveryToggle(evaluation);
  }

  it('restores the original weight and valid total after unchecking recovery', () => {
    toggle(0, true);
    expect(component.evaluations[0].weightPercentage).toBe(0);
    expect(component.totalWeight).toBe(66);
    expect(component.isValid).toBe(false);
    toggle(0, false);
    expect(component.evaluations[0].weightPercentage).toBe(34);
    expect(component.totalWeight).toBe(100);
    expect(component.isValid).toBe(true);
    component.confirm();
    expect(close).toHaveBeenCalledWith(expect.arrayContaining([
      expect.objectContaining({ weightPercentage: 34, isRecovery: false })
    ]));
  });

  it('restores the latest edited value through repeated toggles', () => {
    component.evaluations[0].weightPercentage = 25.5;
    toggle(0, true);
    toggle(0, false);
    expect(component.evaluations[0].weightPercentage).toBe(25.5);
    component.evaluations[0].weightPercentage = 40;
    toggle(0, true);
    toggle(0, false);
    expect(component.evaluations[0].weightPercentage).toBe(40);
  });

  it('keeps weights associated with their row when another row is removed', () => {
    toggle(1, true);
    component.removeEvaluation(0);
    toggle(0, false);
    expect(component.evaluations[0].weightPercentage).toBe(33);
  });

  it('does not invent a previous weight for default recovery rows', () => {
    toggle(3, false);
    expect(component.evaluations[3].weightPercentage).toBe(0);
    expect(component.isValid).toBe(false);
  });
});
