using FluentAssertions;

using WinEnvEdit.Core.Helpers;

using Xunit;

namespace WinEnvEdit.Tests.Helpers;

public class PathListHelperTests {
  #region SplitPathList Tests

  [Fact]
  public void SplitPathList_ValidPaths_ReturnsList() {
    // Act
    var result = PathListHelper.SplitPathList("C:\\path1;C:\\path2;C:\\path3");

    // Assert
    result.Should().HaveCount(3);
    result[0].Should().Be("C:\\path1");
    result[1].Should().Be("C:\\path2");
    result[2].Should().Be("C:\\path3");
  }

  [Fact]
  public void SplitPathList_EmptyString_ReturnsEmptyList() {
    // Act
    var result = PathListHelper.SplitPathList("");

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public void SplitPathList_TrimsWhitespace() {
    // Act
    var result = PathListHelper.SplitPathList("  C:\\path1  ;  C:\\path2  ");

    // Assert
    result.Should().HaveCount(2);
    result[0].Should().Be("C:\\path1");
    result[1].Should().Be("C:\\path2");
  }

  [Fact]
  public void SplitPathList_SkipsEmptyEntries() {
    // Act
    var result = PathListHelper.SplitPathList("C:\\path1;;C:\\path2;");

    // Assert
    result.Should().HaveCount(2);
    result[0].Should().Be("C:\\path1");
    result[1].Should().Be("C:\\path2");
  }

  #endregion

  #region JoinPathList Tests

  [Fact]
  public void JoinPathList_ValidPaths_ReturnsSemicolonDelimited() {
    // Arrange
    var paths = new[] { "C:\\path1", "C:\\path2", "C:\\path3" };

    // Act
    var result = PathListHelper.JoinPathList(paths);

    // Assert
    result.Should().Be("C:\\path1;C:\\path2;C:\\path3");
  }

  [Fact]
  public void JoinPathList_EmptyList_ReturnsEmptyString() {
    // Arrange
    var paths = Array.Empty<string>();

    // Act
    var result = PathListHelper.JoinPathList(paths);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public void JoinPathList_SinglePath_ReturnsPath() {
    // Arrange
    var paths = new[] { "C:\\path1" };

    // Act
    var result = PathListHelper.JoinPathList(paths);

    // Assert
    result.Should().Be("C:\\path1");
  }

  #endregion

  #region ReconcilePathLists Tests

  [Fact]
  public void ReconcilePathLists_SameLists_ReturnsNoChanges() {
    // Arrange
    var current = new List<string> { "C:\\path1", "C:\\path2", "C:\\path3" };
    var newPaths = new List<string> { "C:\\path1", "C:\\path2", "C:\\path3" };

    // Act
    var result = PathListHelper.ReconcilePathLists(current, newPaths);

    // Assert
    result.ItemsToUpdate.Should().BeEmpty();
    result.ItemsToAdd.Should().BeEmpty();
    result.CountToRemove.Should().Be(0);
  }

  [Fact]
  public void ReconcilePathLists_UpdatedItems_ReturnsUpdates() {
    // Arrange
    var current = new List<string> { "C:\\path1", "C:\\path2", "C:\\path3" };
    var newPaths = new List<string> { "C:\\newpath1", "C:\\path2", "C:\\newpath3" };

    // Act
    var result = PathListHelper.ReconcilePathLists(current, newPaths);

    // Assert
    result.ItemsToUpdate.Should().HaveCount(2);
    result.ItemsToUpdate.Should().Contain((0, "C:\\newpath1"));
    result.ItemsToUpdate.Should().Contain((2, "C:\\newpath3"));
    result.ItemsToAdd.Should().BeEmpty();
    result.CountToRemove.Should().Be(0);
  }

  [Fact]
  public void ReconcilePathLists_AddedItems_ReturnsAdds() {
    // Arrange
    var current = new List<string> { "C:\\path1", "C:\\path2" };
    var newPaths = new List<string> { "C:\\path1", "C:\\path2", "C:\\path3", "C:\\path4" };

    // Act
    var result = PathListHelper.ReconcilePathLists(current, newPaths);

    // Assert
    result.ItemsToUpdate.Should().BeEmpty();
    result.ItemsToAdd.Should().HaveCount(2);
    result.ItemsToAdd.Should().Contain("C:\\path3");
    result.ItemsToAdd.Should().Contain("C:\\path4");
    result.CountToRemove.Should().Be(0);
  }

  [Fact]
  public void ReconcilePathLists_RemovedItems_ReturnsRemoveCount() {
    // Arrange
    var current = new List<string> { "C:\\path1", "C:\\path2", "C:\\path3", "C:\\path4" };
    var newPaths = new List<string> { "C:\\path1", "C:\\path2" };

    // Act
    var result = PathListHelper.ReconcilePathLists(current, newPaths);

    // Assert
    result.ItemsToUpdate.Should().BeEmpty();
    result.ItemsToAdd.Should().BeEmpty();
    result.CountToRemove.Should().Be(2);
  }

  [Fact]
  public void ReconcilePathLists_MixedChanges_ReturnsAll() {
    // Arrange
    var current = new List<string> { "C:\\path1", "C:\\path2", "C:\\path3" };
    var newPaths = new List<string> { "C:\\newpath1", "C:\\path2", "C:\\path4", "C:\\path5" };

    // Act
    var result = PathListHelper.ReconcilePathLists(current, newPaths);

    // Assert - Updates at index 0 and 2, Add at index 3
    result.ItemsToUpdate.Should().HaveCount(2);
    result.ItemsToUpdate.Should().Contain((0, "C:\\newpath1"));
    result.ItemsToUpdate.Should().Contain((2, "C:\\path4"));
    result.ItemsToAdd.Should().HaveCount(1);
    result.ItemsToAdd.Should().Contain("C:\\path5");
    result.CountToRemove.Should().Be(0);
  }

  [Fact]
  public void ReconcilePathLists_EmptyToCurrent_ReturnsAdds() {
    // Arrange
    var current = new List<string>();
    var newPaths = new List<string> { "C:\\path1", "C:\\path2" };

    // Act
    var result = PathListHelper.ReconcilePathLists(current, newPaths);

    // Assert
    result.ItemsToUpdate.Should().BeEmpty();
    result.ItemsToAdd.Should().HaveCount(2);
    result.CountToRemove.Should().Be(0);
  }

  [Fact]
  public void ReconcilePathLists_CurrentToEmpty_ReturnsRemoves() {
    // Arrange
    var current = new List<string> { "C:\\path1", "C:\\path2" };
    var newPaths = new List<string>();

    // Act
    var result = PathListHelper.ReconcilePathLists(current, newPaths);

    // Assert
    result.ItemsToUpdate.Should().BeEmpty();
    result.ItemsToAdd.Should().BeEmpty();
    result.CountToRemove.Should().Be(2);
  }

  #endregion
}
