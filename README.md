**database design**

   
    
    CREATE TABLE directories (
        path TEXT PRIMARY KEY,
        last_signature TEXT,        -- 目录的特征值
        last_check_time TIMESTAMP,  -- 最后检查时间
        last_process_time TIMESTAMP,-- 最后处理时间
        status TEXT                 -- 'PENDING', 'PROCESSING', 'COMPLETED', 'NEED_RECHECK'
    );

    CREATE TABLE files (
        path TEXT PRIMARY KEY,
        directory_path TEXT,
        file_size INTEGER,
        modified_time REAL,         -- 文件修改时间
        process_time TIMESTAMP,     -- 处理时间
        status TEXT,               -- 'PENDING', 'PROCESSED', 'FAILED'
        hash TEXT,                 -- 文件内容hash
        FOREIGN KEY (directory_path) REFERENCES directories(path)
    );

**Procedure diagram**:

***Main process***
```mermaid
flowchart TD
    A[开始扫描] --> B{检查目录是否存在于DB}
    B -->|不存在| C[添加目录记录]
    B -->|存在| D{检查目录特征值}
    
    D -->|特征值变化| E[标记目录需重检查]
    D -->|特征值无变化且完成| F[跳过目录]
    D -->|特征值无变化未完成| G[继续处理]
    
    C --> H[扫描目录文件]
    E --> H
    G --> H
    
    H --> I{对每个文件检查}
    I -->|新文件| J[添加文件记录]
    I -->|已存在文件| K{检查文件状态}
    
    K -->|文件已修改| L[标记需重处理]
    K -->|文件未修改| M[保持原状态]
    
    J --> N[处理文件]
    L --> N
    
    N -->|处理成功| O[标记为已处理]
    N -->|处理失败| P[标记为失败]
    
    O --> Q[更新目录状态]
    P --> Q
    
    Q --> R[继续下一个文件/目录]
```

***file status changing***
```mermaid
stateDiagram-v2
    [*] --> PENDING: 新文件添加
    PENDING --> PROCESSING: 开始处理
    PROCESSING --> PROCESSED: 处理成功
    PROCESSING --> FAILED: 处理失败
    PROCESSED --> PENDING: 文件被修改
    FAILED --> PENDING: 重试处理
    PROCESSED --> DELETED: 文件被删除
    FAILED --> DELETED: 文件被删除
    PENDING --> DELETED: 文件被删除
```
***directory changing***
```mermaid
stateDiagram-v2
    [*] --> PENDING: 新目录
    PENDING --> PROCESSING: 开始处理
    PROCESSING --> COMPLETED: 处理完成
    PROCESSING --> NEED_RECHECK: 发现变化
    COMPLETED --> NEED_RECHECK: 特征值变化
    NEED_RECHECK --> PROCESSING: 重新处理
```


***single file process***

```mermaid

flowchart TD
    A[开始处理文件] --> B{文件是否存在}
    B -->|否| C[标记为已删除]
    B -->|是| D{检查文件锁定状态}
    
    D -->|已锁定| E[等待并重试]
    D -->|未锁定| F[获取文件信息]
    
    F --> G{验证文件完整性}
    G -->|不完整| H[标记为失败]
    G -->|完整| I[处理文件内容]
    
    I --> J{处理结果}
    J -->|成功| K[更新处理状态]
    J -->|失败| L[记录错误信息]
    
    K --> M[更新文件元数据]
    L --> N[标记为失败]
    
    E --> O[达到重试上限]
    O -->|是| P[标记为失败]
    O -->|否| D
```