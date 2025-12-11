/**
 * Result type for handling success and failure cases
 * Implements Railway Oriented Programming pattern
 */
export class Result<T, E = string> {
  public readonly isSuccess: boolean;
  public readonly isFailure: boolean;
  private readonly _value?: T;
  private readonly _error?: E;

  private constructor(isSuccess: boolean, value?: T, error?: E) {
    this.isSuccess = isSuccess;
    this.isFailure = !isSuccess;
    this._value = value;
    this._error = error;

    Object.freeze(this);
  }

  public get value(): T {
    if (this.isFailure) {
      throw new Error('Cannot get value from failed result');
    }
    return this._value as T;
  }

  public get error(): E {
    if (this.isSuccess) {
      throw new Error('Cannot get error from successful result');
    }
    return this._error as E;
  }

  public static ok<T, E = string>(value: T): Result<T, E> {
    return new Result<T, E>(true, value);
  }

  public static fail<T, E = string>(error: E): Result<T, E> {
    return new Result<T, E>(false, undefined, error);
  }

  public static combine<T = any>(results: Result<T>[]): Result<T[]> {
    const failed = results.find((result) => result.isFailure);
    if (failed) {
      return Result.fail(failed.error);
    }
    return Result.ok(results.map((result) => result.value));
  }
}
