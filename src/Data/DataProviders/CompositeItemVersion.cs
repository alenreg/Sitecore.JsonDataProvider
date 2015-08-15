namespace Sitecore.Support.Data.DataProviders
{
  using System;
  using System.Collections;
  using System.Collections.Generic;

  using Sitecore.Diagnostics;

  public abstract class CompositeItemVersion
  {
    [NotNull]
    public CompositeItemLanguage ItemLanguage { get; private set; }

    public abstract int Number { get; }

    [NotNull]
    public abstract IDictionary<string, string> Fields { get; }

    [NotNull]
    public abstract CompositeItemVersion CreateFrom([NotNull] CompositeItemVersion existingVersion, int newNumber);

    public void Initialize([NotNull] CompositeItemLanguage language)
    {
      Assert.ArgumentNotNull(language, "language");

      this.ItemLanguage = language;
    }
  }
}