/**
 * Base class for Value Objects
 * Value Objects are immutable and compared by their values
 */
export abstract class ValueObject<T> {
  protected readonly _value: T;

  constructor(value: T) {
    this._value = Object.freeze(value);
  }

  public getValue(): T {
    return this._value;
  }

  public equals(vo: ValueObject<T>): boolean {
    if (vo === null || vo === undefined) {
      return false;
    }
    return JSON.stringify(this._value) === JSON.stringify(vo._value);
  }
}
