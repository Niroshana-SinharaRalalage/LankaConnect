/**
 * Base class for Entities
 * Entities are identified by their ID, not by their attributes
 */
export abstract class Entity<T> {
  protected readonly _id: string;
  protected readonly props: T;

  constructor(props: T, id: string) {
    this._id = id;
    this.props = props;
  }

  public get id(): string {
    return this._id;
  }

  public equals(entity?: Entity<T>): boolean {
    if (entity === null || entity === undefined) {
      return false;
    }

    if (this === entity) {
      return true;
    }

    return this._id === entity._id;
  }
}
