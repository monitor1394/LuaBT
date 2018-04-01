
local BTNode = bt.Class("BTNode")
bt.BTNode = BTNode

function BTNode:ctor()
    self.name = "BTNode"
    self.status = bt.Status.Resting
    self.isChecked = false
    self.maxInConnections = 1
    self.maxOutConnections = 0
    self.inConnections = {}
    self.inSize = 0
    self.outConnections = {}
    self.outSize = 0
end

function BTNode:addInConnection(connection)
    table.insert( self.inConnections, connection )
    self.inSize = self.inSize + 1
end

function BTNode:addOutConnection(connection)
    table.insert( self.outConnections, connection )
    self.outSize = self.outSize + 1
end

function BTNode:execute(agent,blackboard)
    if self.isChecked then
        print("Infinite Loop. Please check for other errors that may have caused this in the log before this.")
        return bt.Status.Failure
    end
    self.isChecked = true
    self.status = onExecute(agent,blackboard)
    self.isChecked = false
    return slef.status
end

function BTNode:reset(recursively)
    if recursively == nil then
        recursively = true
    end
    self:onReset()
    self.status = bt.Status.Resting
    self.isChecked = true
    local size = #self.outConnections
    for i=1,size,1 do
        self.outConnections[i]:reset(recursively)
    end
    self.isChecked = false
end

function BTNode:onExecute(agent,blackboard)
    return self.status
end

function BTNode:onReset()
end

function BTNode:onGraphStarted()
end

function BTNode:onGraphStoped()
end

function BTNode:onGraphPaused()
end

function BTNode:onGraphUnpaused()
end



