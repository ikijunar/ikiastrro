-- 028_create_temporary_friendship_rules.sql
-- Transcribes ClassicalDignity.IsTemporaryFriend verbatim: friend if sign-distance (1=same
-- sign) is 2,3,4,10,11,12; enemy otherwise (1,5,6,7,8,9 -- note 7 counts as enemy in this
-- classical rule, same as 1/5/6/8/9, despite 7 being the universal aspect house elsewhere).

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Rule_TemporaryFriendshipDistance')
BEGIN
    CREATE TABLE tbl_Rule_TemporaryFriendshipDistance
    (
        RuleSetId    TINYINT NOT NULL REFERENCES tbl_Rule_Sets(Id),
        SignDistance TINYINT NOT NULL CHECK (SignDistance BETWEEN 1 AND 12),
        IsFriend     BIT     NOT NULL,
        PRIMARY KEY (RuleSetId, SignDistance)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Rule_TemporaryFriendshipDistance)
BEGIN
    INSERT INTO tbl_Rule_TemporaryFriendshipDistance (RuleSetId, SignDistance, IsFriend)
    VALUES (1,1,0),(1,2,1),(1,3,1),(1,4,1),(1,5,0),(1,6,0),(1,7,0),(1,8,0),(1,9,0),(1,10,1),(1,11,1),(1,12,1);
END
GO
