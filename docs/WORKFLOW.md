# 標準開発フロー

1. 目的、完了条件、対象外、確認方法を記したIssueを作成する。
2. `main`を`git pull --ff-only origin main`で同期し、working treeがcleanであることを確認する。
3. Issueに対応する `feature/`、`fix/`、`refactor/`、`docs/`、`chore/` branchを作る。
4. Issue範囲内の最小変更を行う。
5. restoreとRelease buildを実行する。GUI・音声に関係する場合はWindows上で手動確認する。
6. `git diff --check`、`git diff`、`git status`で変更と生成物を確認する。
7. commitしてbranchをpushし、IssueをlinkしたPull Requestを作る。
8. CIとreviewを確認し、成功後にSquash mergeする。
9. localの`main`へ戻り、`git pull --ff-only origin main`で同期する。

CIはコンパイル可能性を確認しますが、WPF描画、入力操作、音声出力の代替にはなりません。手動確認できなかった項目はPull Requestに明記します。
