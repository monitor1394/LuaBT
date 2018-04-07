
-----------------------------------------------------------------------------------------
-- Composites
-- BinarySelector

-- Quick way to execute the left, or the right child node based on a Condition Task evaluation.
-----------------------------------------------------------------------------------------

local BinarySelector = bt.Class("BinarySelector",bt.BTComposite)
bt.BinarySelector = BinarySelector

function BinarySelector:ctor()
    bt.BTComposite.ctor(self)
    self.name = "BinarySelector"
    self.dynamic = false
    self.condition = nil
    self.succeedIndex = 1
    self.maxOutConnections = 2
end

function BinarySelector:init(jsonData)
    if jsonData.dynamic then
        self.dynamic = jsonData.dynamic
    end
    if jsonData._condition then
        local Cls = bt.getCls(jsonData._condition["$type"], jsonData._condition)
        self.condition = Cls.new()
        self.condition:init(jsonData._condition)
    end
end

function BinarySelector:onExecute(agent,blackboard)
    if self.condition == nil or #self.outConnections < 2 then
        return bt.Status.Failure
    end
    if self.dynamic or self.status == bt.Status.Resting then
        local lastIndex = self.succeedIndex
        if self.condition:checkCondition(agent,blackboard) then
            self.succeedIndex = 1
        else
            self.succeedIndex = 2
        end
        if self.succeedIndex ~= lastIndex then 
            self.outConnections[lastIndex]:reset(true)
        end
    end
    return self.outConnections[self.succeedIndex]:execute(agent,blackboard)
end

function BinarySelector:destroy()
    if self.condition ~= nil then
        self.condition:destroy()
        self.condition = nil
    end
end