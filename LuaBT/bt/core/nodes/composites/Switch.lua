
-----------------------------------------------------------------------------------------
-- Composites
-- Switch

-- Executes ONE child based on the provided int or enum and return it's status. If set the 
-- Dynamic and 'case' change while a child is running, that child will be interrupted 
-- before the new child is executed.
-----------------------------------------------------------------------------------------

local Switch = bt.Class("Switch",bt.BTComposite)
bt.Switch = Switch

CaseSelectionMode =
{
    IndexBased      = 0,
    EnumBased       = 1,
}

OutOfRangeMode =
{
    ReturnFailure   = 0,
    LoopIndex       = 1,
}

function Switch:ctor()
    bt.BTComposite.ctor(self)
    self.name = "Switch"
    self.dynamic = false
    self.intCase = 0
    self.selectionMode = CaseSelectionMode.IndexBased
    self.outOfRangeMode = OutOfRangeMode.LoopIndex
    self.current = 1
    self.runningIndex = 1
end

function Switch:init(jsonData)
    if jsonData.dynamic then
        self.dynamic = jsonData.dynamic
    end
    if jsonData.intCase ~= nil then
        self.intCase = jsonData.intCase._value + 1
    end
    if jsonData.outOfRangeMode then
        self.outOfRangeMode = OutOfRangeMode[jsonData.outOfRangeMode]
    end
    if jsonData.selectionMode then
        self.selectionMode = CaseSelectionMode[jsonData.selectionMode]
    end
    if self.selectionMode == CaseSelectionMode.EnumBased then
        print("Switch ERROR:not suport CaseSelectionMode.EnumBased")
    end
end

function Switch:onExecute(agent,blackboard)
    if self.selectionMode == CaseSelectionMode.EnumBased 
       or #self.outConnections == 0 then
        return bt.Status.Failure
    end
    if self.status == bt.Status.Resting or self.dynamic then
        self.current = self.intCase
        if self.current > #self.outConnections and 
           self.outOfRangeMode == OutOfRangeMode.LoopIndex then
            self.current = self.current % (#self.outConnections)
        end
    end
    if self.runningIndex ~= self.current then
        self.outConnections[self.runningIndex]:reset(true)
    end
    if self.current < 1 or self.current > #self.outConnections then
        return bt.Status.Failure
    end
    self.status = self.outConnections[self.current]:execute(agent,blackboard)
    if self.status == bt.Status.Running then
        self.runningIndex = self.current
    end
    return self.status
end